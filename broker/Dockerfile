FROM eclipse-mosquitto:latest

COPY mosquitto/config/ mosquitto/config/

ARG TLS_SERVER_KEY

RUN start='-----BEGIN PRIVATE KEY-----' && \
    decoded_key=$(echo $TLS_SERVER_KEY|base64 -d) && \
    end='-----END PRIVATE KEY-----'&& \
    echo ${start} > mosquitto/config/certs/server-key.pem && \
    echo ${decoded_key} >> mosquitto/config/certs/server-key.pem && \
    echo ${end} >> mosquitto/config/certs/server-key.pem

EXPOSE 1883

USER 1883

CMD ["mosquitto", "-p", "1883", "-c", "mosquitto/config/mosquitto.conf"]
