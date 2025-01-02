#!/bin/bash
# Substitute environment variables in the index.html file using the values in the current container environment

# Check if index.html exists in the correct location
if [ ! -f /app/index.html ]; then
  echo "Couldn't find index.html"
fi

envsubst '
  ${VITE_AI_CONNECTION_STRING}
  ${VITE_BACKEND_URL}
  ${VITE_BACKEND_API_SCOPE}
  ${VITE_FRONTEND_URL}
  ${VITE_FRONTEND_BASE_ROUTE}
  ${VITE_AD_CLIENT_ID}
  ${VITE_AD_TENANT_ID}
  ' </app/index.html >/app/tmp.html
mv /app/tmp.html /app/index.html
# Start Nginx
echo $(date) Starting Nginx…
nginx -g "daemon off;"
