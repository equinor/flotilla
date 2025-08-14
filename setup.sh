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
#-----------------------------

#-------- BACKEND ------------
echo "-------- BACKEND ------------"
echo -e "Setting up backend .env root..."

if [ -f $flotilla_dir/.env ]; then
    echo -e "WARNING: The file '$flotilla_dir/.env' already exists, it will be overwritten if the operation continues."
    echo -e "Is this ok? (Y/n)"

    read reply
    if [ "$reply" = "n" ] || [ "$reply" = "N" ]; then
        echo -e "\nBackend setup - Aborted!\n"
        backend_abort="true"
    fi
fi

if [ "$backend_abort" != "true" ]; then
    echo -e "Flotilla azure client secret needed for backend dockerization."
    echo -en "Input Flotilla Azure Client Secret (copy-paste from KeyVault):\n"

    while [ true ]
    do
        read -s az_client_secret

        if [ -z "$az_client_secret" ]; then
            echo "Azure client secret cannot be empty"
            echo "Try again:"
        else
            break
        fi
    done


    echo "FLOTILLA_CLIENT_SECRET='$az_client_secret'" > $flotilla_dir/.env
    echo -e "Added client secret to .env file"
    dotnet user-secrets set "AzureAd:ClientSecret" $az_client_secret --project backend/api > /dev/null
    echo -e "Added client secret to ASP.NET secret manager"


    echo -e "Flotilla broker server key needed for backend dockerization."
    echo -en "Input Flotilla Broker Server Key (copy-paste from KeyVault):\n"

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


    echo "FLOTILLA_BROKER_SERVER_KEY='$broker_server_key'" >> $flotilla_dir/.env
    echo -e "Added broker server key to .env file"

fi

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

    echo -e "Azure client id needed for backend dockerization."
    echo -en "Azure Client Id (copy-paste from KeyVault):\n"

    while [ true ]
    do
        read -s az_client_id

        if [ -z "$az_client_id" ]; then
            echo "Azure client id cannot be empty"
            echo "Try again:"
        else
            break
        fi
    done

    echo "AZURE_CLIENT_ID=$az_client_id" >> $flotilla_dir/backend/api/.env
    echo -e "Added client id to .env file"

    echo -e "Azure tenant id needed for backend dockerization."
    echo -en "Azure Tenant Id (copy-paste from KeyVault):\n"

    while [ true ]
    do
        read -s az_tenant_id

        if [ -z "$az_tenant_id" ]; then
            echo "Azure tenant id cannot be empty"
            echo "Try again:"
        else
            break
        fi
    done

    echo "AZURE_TENANT_ID=$az_tenant_id" >> $flotilla_dir/backend/api/.env
    echo -e "Added tenant id to .env file"

    echo -e "Azure client secret needed for backend dockerization."
    echo -en "Azure Client Secret (copy-paste from KeyVault):\n"

    while [ true ]
    do
        read -s az_client_secret

        if [ -z "$az_client_secret" ]; then
            echo "Azure client secret cannot be empty"
            echo "Try again:"
        else
            break
        fi
    done

    echo "AZURE_CLIENT_SECRET=$az_client_secret" >> $flotilla_dir/backend/api/.env
    echo -e "Added client secret to .env file"

    echo -e "A username is needed for local development with SignalR"
    echo -en "Input a username for yourself (this only needs to be unique within your development environment):\n"

    while [ true ]
    do
        read -s local_dev_username

        if [ -z "$local_dev_username" ]; then
            echo "The local dev username cannot be empty"
            echo "Try again:"
        else
            break
        fi
    done

    echo "Local__DevUserId=$local_dev_username" >> $flotilla_dir/backend/api/.env
    echo -e "Added local development username to .env file"

    echo -e "Backup setup - Done!"
    echo -e "-----------------------------\n"
fi

#-----------------------------

#--------- BROKER ------------
echo "--------- BROKER ------------"
echo -e "Setting up broker ..."

if [ -f $flotilla_dir/.env ]; then
    echo -e "WARNING: The file '$flotilla_dir/.env' already exists, it will be overwritten if the operation continues."
    echo -e "Is this ok? (Y/n)"

    read reply
    if [ "$reply" = "n" ] || [ "$reply" = "N" ]; then
        echo -e "\Broker setup - Aborted!\n"
        broker_abort="true"
    fi
fi
if [ "$broker_abort" != "true" ]; then
    echo -e "MQTT TLS Server key needed for the broker to communicate using TLS"
    echo -en "Input MQTT broker server key (copy-paste from KeyVault):\n"
    read -s broker_server_key

    # Save to .env file
    echo -e "FLOTILLA_BROKER_SERVER_KEY='$broker_server_key'" >> $flotilla_dir/.env

    echo -e "Added broker server key to .env file"
    echo -e "Broker setup - Done!"
    echo -e "-----------------------------\n"
    #-----------------------------


    echo -e "Flotilla setup - Done!"
    echo -e "-----------------------------"
fi
