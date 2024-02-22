#!/bin/bash


export ASPNET_APP_IMAGE=$1
export ASPNET_APP_NAME=$2
export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_CERT_PASSWORD=$3
export ASPNETCORE_URLS=https://+:443;http://+:80
export ASPNETCORE_CERTIFICATE_PATH=/https/${ASPNET_APP_NAME}.pfx
export REDIS_DATABASE_URL=uploader-redis:6379

# NFS Server Details
NFS_SERVER="10.40.1.221"
NFS_SHARE="/mnt/sdb/no-entry"
MAIL_MOUNT_POINT="/mnt/mailboxes/"
CERTIFICATE_MOUNT_POINT="/mnt/certificates"

# Check or create mount point
if [ ! -d $MAIL_MOUNT_POINT ]; then
    echo "----------CREATING MOUNT POINT: $MOUNT_POINT ----------"
    sudo mkdir -p $MAIL_MOUNT_POINT
    if [ $? -ne 0 ]; then
        echo "AN ERROR OCCURRED CREATING THE MOUNT POINT: $MAIL_MOUNT_POINT"
        exit 1
    else
      echo "----------MOUNT POINT: $MAIL_MOUNT_POINT  CREATED SUCCESSFULLY----------"
    fi
fi

if [ ! -d $CERTIFICATE_MOUNT_POINT ]; then
    echo "----------CREATING CERTIFICATE MOUNT POINT: $CERTIFICATE_MOUNT_POINT ----------"
    sudo mkdir -p $CERTIFICATE_MOUNT_POINT
    if [ $? -ne 0 ]; then
        echo "AN ERROR OCCURRED CREATING THE CERTIFICATE MOUNT POINT: $CERTIFICATE_MOUNT_POINT"
        exit 1
    else
      echo "----------CERTIFICATE MOUNT POINT: $CERTIFICATE_MOUNT_POINT CREATED SUCCESSFULLY----------"
    fi
fi

# Check if nfs-common is installed
if ! dpkg -l | grep -q nfs-common; then
    echo "nfs-common is not installed. Attempting to install..."
    sudo apt install -y nfs-common
    if [ $? -ne 0 ]; then
        echo "Failed to install nfs-common. Exiting."
        exit 1
    fi
fi

# Check if the nfs volume is mounted.
if mountpoint -q $MAIL_MOUNT_POINT && mountpoint -q $CERTIFICATE_MOUNT_POINT; then
    echo "............NFS SHARES MOUNTED, SKIPPING MOUNT PROCESS............"
else
    # Attempt to mount the NFS shares
    echo "............ATTEMPTING TO MOUNT NFS SHARES............"
    sudo mount $NFS_SERVER:$NFS_SHARE/mailboxes $MAIL_MOUNT_POINT
    if [ $? -eq 0 ]; then
        echo "MAILBOXES NFS MOUNTED SUCCESSFULLY"
    else
        echo "Failed to mount MAILBOXES NFS share."
        exit 1
    fi

    sudo mount $NFS_SERVER:$NFS_SHARE/certificates $CERTIFICATE_MOUNT_POINT
    if [ $? -eq 0 ]; then
        echo "CERTIFICATES NFS MOUNTED SUCCESSFULLY"
    else
        echo "Failed to mount CERTIFICATES NFS share."
        exit 1
    fi
fi

# Run the web app

echo "---------- STARTING MIGRATE-CLIENT-PYTHON-WEB-API ----------"

echo "Neo4knust!" | docker login dreg.knust.edu.gh -u neo --password-stdin
docker compose -f prod.yaml up --build --detach


