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
echo -e "Setting up backend ..."

if [ -f $flotilla_dir/.env ]; then
    echo -e "WARNING: The file '$flotilla_dir/.env' already exists, it will be overwritten if the operation continues."
    echo -e "Is this ok? (Y/n)"

    read reply
    if [ "$reply" = "n" ] || [ "$reply" = "N" ]; then
        echo -e "\nSETUP ABORTED\n"
        exit 1
    fi
fi
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

echo -e "Backup setup - Done!"
echo -e "-----------------------------\n"
#-----------------------------

#--------- BROKER ------------
echo "--------- BROKER ------------"
echo -e "Setting up broker ..."
echo -e "MQTT TLS Server key needed for the broker to communicate using TLS"
echo -en "Input MQTT broker server key (copy-paste from KeyVault):\n" 
read -s broker_server_key

start='-----BEGIN PRIVATE KEY-----'
end='-----END PRIVATE KEY-----'

# remove 'end' line
key_content=${broker_server_key%$end}

# remove 'begin' line
key_content=${key_content#$start}

# recompose with the correct newlines:
complete_key='\n'$start'\n'$key_content'\n'$end

# save to .env file
echo -e "FLOTILLA_BROKER_SERVER_KEY='$complete_key'" >> $flotilla_dir/.env

echo -e "Added broker server key to .env file"
echo -e "Broker setup - Done!"
echo -e "-----------------------------\n"
#-----------------------------


echo -e "Flotilla setup - Done!"
echo -e "-----------------------------"
