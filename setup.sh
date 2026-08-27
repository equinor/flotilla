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

    cp $flotilla_dir/backend/api/.env.example $flotilla_dir/backend/api/.env
    echo -e "Created backend/api/.env file from backend/api/.env.example"

    echo -e "Backend setup - Done!"
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
        echo -e "\nBroker setup - Aborted!\n"
        broker_abort="true"
    fi
fi
if [ "$broker_abort" != "true" ]; then
    # Local development gets its own throwaway credentials. Nothing here is
    # shared with a deployed environment, so a laptop cannot leak one.
    credentials_dir=$flotilla_dir/broker/.local-credentials
    echo -e "Generating local broker credentials in $credentials_dir ..."

    rm -rf "$credentials_dir"
    $flotilla_dir/broker/scripts/generate-mqtt-credentials.sh \
        -o "$credentials_dir" broker localhost host.docker.internal IP:127.0.0.1 > /dev/null

    # docker compose reads broker/.env through env_file, which cannot hold a
    # multi-line value, so the PEM bodies go in on a single line. The broker
    # reassembles them; see broker/entrypoint.sh.
    pem_body() {
        grep -v -- '-----' "$1" | tr -d '\n'
    }

    {
        echo "TLS_SERVER_KEY='$(pem_body "$credentials_dir/server-key.pem")'"
        echo "TLS_SERVER_CERT='$(pem_body "$credentials_dir/server-cert.pem")'"
        echo "TLS_CA_CERT='$(pem_body "$credentials_dir/ca-cert.pem")'"
        cat "$credentials_dir/mqtt-passwords.env"
    } > $flotilla_dir/broker/.env

    echo -e "Created broker/.env with freshly generated local credentials"
    echo -e "\nThe backend connects as the 'flotilla' user. Put its password in the"
    echo -e "ASP.NET Secret Manager as Mqtt:Password:\n"
    echo -e "  cd backend/api && dotnet user-secrets set \"Mqtt:Password\" \"$(sed -n 's/^flotilla=//p' "$credentials_dir/passwords")\"\n"

    echo -e "Broker setup - Done!"
    echo -e "-----------------------------\n"
    #-----------------------------


    echo -e "Flotilla setup - Done!"
    echo -e "-----------------------------"
fi
