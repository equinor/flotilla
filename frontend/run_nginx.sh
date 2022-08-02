#!/bin/bash
# Substitute environment variables in the index.html file using the values in the current container environment
envsubst '
  ${BACKEND_URL}
  ${FRONTEND_URL}
  ${AD_CLIENT_ID}
  ${AD_TENANT_ID}
  ' </app/index.html >/app/tmp.html
mv /app/tmp.html /app/index.html
# Start Nginx
echo $(date) Starting Nginx…
nginx -g "daemon off;"

