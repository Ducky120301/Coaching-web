location = /config.json { add_header Cache-Control "no-store"; try_files $uri =404; }
location /api/ {
  client_max_body_size 6m; # @BR-f02-import-excel-015: để API tự từ chối file > 5 MB bằng import.file_too_large
  proxy_pass http://api:8080;
  proxy_set_header Host $host;
  proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
  proxy_set_header X-Forwarded-Proto $scheme;
}
location = /health { proxy_pass http://api:8080/health; }
location / { try_files $uri $uri/ /index.html; }
