FROM eclipse-mosquitto:2.0.22@sha256:212f89e1eaeb2c322d6441b64396e3346026674db8fa9c27beac293405c32b3c

COPY mosquitto/config/ mosquitto/config/
COPY entrypoint.sh entrypoint.sh

# Credentials are assembled here on startup, so this must be writable.
RUN mkdir -p mosquitto/config/runtime
RUN chown -R mosquitto:mosquitto mosquitto/config/runtime
RUN chmod 0700 mosquitto/config/runtime
RUN chown -R mosquitto:mosquitto mosquitto/config/passwd_file
RUN chmod 0600 mosquitto/config/passwd_file
RUN chown -R mosquitto:mosquitto mosquitto/config/access_control
RUN chmod 0700 mosquitto/config/access_control
EXPOSE 1883

# The mosquitto user, which is uid 1883 in this image. The deployments set the
# same numeric id, since Kubernetes cannot resolve a name.
USER mosquitto

CMD ./entrypoint.sh
