FROM eclipse-mosquitto:latest

COPY mosquitto/config/mosquitto.conf mosquitto/config/mosquitto.conf
COPY mosquitto/config/passwd_file mosquitto/config/passwd_file

EXPOSE 1883 9001

CMD ["mosquitto", "-p", "1883", "-c", "mosquitto/config/mosquitto.conf"]
