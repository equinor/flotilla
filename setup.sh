#!/bin/bash

# Usage: ./setup.sh [--rotate-broker-credentials]
#
#   --rotate-broker-credentials  Mint a new set of local broker credentials even
#                                if the current ones are fine. Every password
#                                changes, so every local client needs the new
#                                one; this script updates the backend's.

rotate_broker_credentials=""

for argument in "$@"; do
    case "$argument" in
        --rotate-broker-credentials) rotate_broker_credentials="-f" ;;
        *)
            echo -e "Unknown argument '$argument'"
            sed -n '3,8p' "$0"
            exit 1
            ;;
    esac
done

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

# Credentials are reused unless they are missing or close to expiring, so this is
# safe to re-run: the password written below stays valid. Pass
# --rotate-broker-credentials to mint a new set deliberately.
if ! $flotilla_dir/broker/scripts/ensure-local-credentials.sh $rotate_broker_credentials > /dev/null; then
    echo -e "\nBroker setup - Failed!\n"
    exit 1
fi

echo -e "Broker setup - Done!"
echo -e "-----------------------------\n"
#-----------------------------

#--- BACKEND MQTT PASSWORD ---
echo "--- BACKEND MQTT PASSWORD ---"

# The backend connects to the broker as the 'flotilla' user. Written every run,
# and outside the abort check above, so declining the .env overwrite still leaves
# a password that matches the broker rather than a stale one.
backend_env=$flotilla_dir/backend/api/.env
mqtt_password=$(sed -n 's/^flotilla=//p' $flotilla_dir/broker/.local-credentials/passwords)

# Replace-or-append, rather than `sed -i`, whose syntax differs between BSD and
# GNU. The temporary file is next to the target, so the move cannot cross
# devices.
{
    grep -v -e '^Mqtt__Password=' -e '^KeyVault__UseKeyVault=' "$backend_env"
    echo "Mqtt__Password=$mqtt_password"
    # The key vault is read after the environment variables, so its shared
    # Mqtt--Password would override the local one. Local development uses the
    # generated credentials instead; the Tilt stack sets this the same way.
    echo "KeyVault__UseKeyVault=false"
} > $backend_env.tmp
mv $backend_env.tmp $backend_env
chmod 0600 $backend_env

echo -e "Wrote Mqtt__Password and KeyVault__UseKeyVault to backend/api/.env"

echo -e "Backend MQTT password - Done!"
echo -e "-----------------------------\n"
#-----------------------------

echo -e "Flotilla setup - Done!"
echo -e "-----------------------------"
