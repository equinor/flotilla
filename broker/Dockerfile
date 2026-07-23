FROM eclipse-mosquitto:2.0.22

COPY mosquitto/config/ mosquitto/config/
COPY entrypoint.sh entrypoint.sh

RUN touch mosquitto/config/certs/server-key.pem
RUN chown -R 1883:1883 mosquitto/config/certs/server-key.pem
RUN chown -R mosquitto:mosquitto mosquitto/config/passwd_file
RUN chmod 0700 mosquitto/config/passwd_file
RUN chown -R mosquitto:mosquitto mosquitto/config/access_control
RUN chmod 0700 mosquitto/config/access_control
EXPOSE 1883

USER 1883

CMD ./entrypoint.sh
