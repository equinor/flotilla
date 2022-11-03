#!/bin/sh
# Set certificate key based on environment variable
# TLS_SERVER_KEY

start='-----BEGIN PRIVATE KEY-----'
end='-----END PRIVATE KEY-----'
echo ${start} > mosquitto/config/certs/server-key.pem
echo ${TLS_SERVER_KEY} | tr -d "'" >> mosquitto/config/certs/server-key.pem
echo ${end} >> mosquitto/config/certs/server-key.pem

mosquitto -p 1883 -c mosquitto/config/mosquitto.conf
