@Library('shared-lib-two')

//library identifier: 'jenkins-shared-library@master', retriever: modernSCM(
//    [$class: GitSCMSource,
//    remote: 'https-something',
//    credentialsId: 'some-credentials in jenkins'
//  ]
//)

def shouldExecuteOnBranch(branchName) {
    return BRANCH_NAME == branchName
}

def imageTag = "dreg.knust.edu.gh/neo/migrate-client-aspnet-core:latest"
pipeline {
    agent any
    environment {
        CERT_PASS = credentials('certificate-password')
    }
    stages {
        stage('build-and push docker-image'){
            when {
                expression {
                    shouldExecuteOnBranch("dev")
                }
            }
            steps {
                script {
                    buildDockerImage "$imageTag"
                    dockerLogin ("knust-docker-registry", "https://dreg.knust.edu.gh")
                    pushDockerImage "$imageTag"
                }
            }
        }

        stage('deploy-app-to-test-server'){
            when{
                expression {
                    shouldExecuteOnBranch("dev")
                }
            }
            steps {
                script {

                    def testServer = "ubuntu@10.40.1.98"
                    def storageServer = "10.40.1.221:/mnt/sdb/no-entry/certificates"
                    def project = [
                            name : 'MigrateClient',
                            creds : "$CERT_PASS"
                    ]
                    def mountPoint = "/home/ubuntu"
                    // generateDevelopmentCertificate(project, storageServer, mountPoint)
                    def scriptCmd = "bash ./start-api.sh $imageTag $project.name $project.creds"

                    sshagent(['ubuntu']){
                        sh "scp start-api.sh ${testServer}:/home/ubuntu/"
                        sh "scp prod.yaml ${testServer}:/home/ubuntu/"
                        sh "ssh -o StrictHostKeyChecking=no ${testServer} ${scriptCmd}"
                    }
                }
            }
        }

    }
}