#!/bin/bash

echo -e "-------- FLOTILLA -----------"
echo -e "Running dev setup for Flotilla...\n"

flotilla_dir=$(dirname $0)

#-------- FRONTEND -----------
echo "-------- FRONTEND -----------"
echo -e "Setting up frontend ..."

if [ -f $flotilla_dir/frontend/.env ]; then
    echo -e "WARNING: The file '$flotilla_dir/frontend/.env' already exists, it will be overwritten if the operation continues."
    echo -e "Is this ok? (Y/n)"

    read reply
    if [ "$reply" = "n" ] || [ "$reply" = "N" ]; then
        echo -e "\nFrontend setup - Aborted!"
        frontend_abort="true"
    fi
fi
if [ "$frontend_abort" != "true" ]; then
    cp $flotilla_dir/frontend/.env.example $flotilla_dir/frontend/.env
    echo -e "Created frontend/.env file from frontend/.env.example"
    echo -e "Frontend setup - Done!"
fi

echo -e "-----------------------------\n"

#-------- BACKEND ------------
echo "-------- BACKEND ------------"
echo -e "Setting up backend .env /backend/api..."

backend_abort="false"

if [ -f $flotilla_dir/backend/api/.env ]; then
    echo -e "WARNING: The file '$flotilla_dir/backend/api/.env' already exists, it will be overwritten if the operation continues."
    echo -e "Is this ok? (Y/n)"

    read reply
    if [ "$reply" = "n" ] || [ "$reply" = "N" ]; then
        echo -e "\nBackend setup - Aborted!\n"
        backend_abort="true"
    fi
fi

if [ "$backend_abort" != "true" ]; then

    echo "Local__DevUserId=$local_dev_username" >> $flotilla_dir/backend/api/.env
    echo -e "Added local development username to .env file"

    echo -e "Backup setup - Done!"
    echo -e "-----------------------------\n"
fi

#-----------------------------

#--------- BROKER ------------
echo "--------- BROKER ------------"
echo -e "Setting up broker ..."

if [ -f $flotilla_dir/broker/.env ]; then
    echo -e "WARNING: The file '$flotilla_dir/broker/.env' already exists, it will be overwritten if the operation continues."
    echo -e "Is this ok? (Y/n)"

    read reply
    if [ "$reply" = "n" ] || [ "$reply" = "N" ]; then
        echo -e "\Broker setup - Aborted!\n"
        broker_abort="true"
    fi
fi
if [ "$broker_abort" != "true" ]; then
    echo -e "Broker server key needed for broker dockerization."
    echo -en "Input MQTT broker server key (copy-paste from KeyVault):\n"

    while [ true ]
    do
        read -s broker_server_key

        if [ -z "$broker_server_key" ]; then
            echo "Flotilla broker server key cannot be empty"
            echo "Try again:"
        else
            break
        fi
    done


    echo "TLS_SERVER_KEY='$broker_server_key'" >> $flotilla_dir/broker/.env
    echo -e "Added broker server key to .env file"

    echo -e "Added broker server key to .env file"
    echo -e "Broker setup - Done!"
    echo -e "-----------------------------\n"
    #-----------------------------


    echo -e "Flotilla setup - Done!"
    echo -e "-----------------------------"
fi
