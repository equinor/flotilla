#!/bin/bash
# Substitute environment variables in the index.html file using the values in the current container environment
envsubst '
  ${REACT_APP_BACKEND_URL}
  ${REACT_APP_BACKEND_API_SCOPE}
  ${REACT_APP_FRONTEND_URL}
  ${REACT_APP_FRONTEND_BASE_ROUTE}
  ${REACT_APP_AD_CLIENT_ID}
  ${REACT_APP_AD_TENANT_ID}
  ' </app/index.html >/app/tmp.html
mv /app/tmp.html /app/index.html
# Start Nginx
echo $(date) Starting Nginx…
nginx -g "daemon off;"

