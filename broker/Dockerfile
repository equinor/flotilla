FROM eclipse-mosquitto:latest

COPY mosquitto/config/ mosquitto/config/

EXPOSE 1883 9001

USER 1883

CMD ["mosquitto", "-p", "1883", "-c", "mosquitto/config/mosquitto.conf"]
