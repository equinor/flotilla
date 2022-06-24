FROM eclipse-mosquitto:latest

COPY mosquitto/config/ mosquitto/config/

ARG TLS_SERVER_KEY

RUN echo "${TLS_SERVER_KEY}" > mosquitto/config/certs/server-key.pem

EXPOSE 1883

USER 1883

CMD ["mosquitto", "-p", "1883", "-c", "mosquitto/config/mosquitto.conf"]
