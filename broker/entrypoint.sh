#!/bin/sh

if [ "$MQTT_ALLOW_INSECURE_LOCAL_CONNECTIONS" = "true" ]; then
    echo "WARNING: MQTT TLS disabled — insecure connections allowed. Do NOT use in production."
    mosquitto -p 1883 -c mosquitto/config/mosquitto-notls.conf
else
    # Reconstruct the TLS private key from environment variable
    start='-----BEGIN PRIVATE KEY-----'
    end='-----END PRIVATE KEY-----'
    echo ${start} > mosquitto/config/certs/server-key.pem
    echo ${TLS_SERVER_KEY} | tr -d "'" >> mosquitto/config/certs/server-key.pem
    echo ${end} >> mosquitto/config/certs/server-key.pem

    mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
fi
