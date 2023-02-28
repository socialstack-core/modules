Elastic v8 
==========

Low on disk space 

https://mincong.io/2021/04/10/disk-watermarks-in-elasticsearch/#:~:text=There%20are%20three%20disk%20watermarks%20in%20Elasticsearch%3A%20low%2C,enough%20disk%20space%20and%20avoid%20disk%20full%20problems.


Install locally windows

https://www.elastic.co/guide/en/elasticsearch/reference/current/zip-windows.html

=============================


Install linux 

https://www.elastic.co/guide/en/elasticsearch/reference/current/deb.html

https://stackoverflow.com/questions/58656747/elasticsearch-job-for-elasticsearch-service-failed

sudo systemctl start elasticsearch.service
sudo systemctl stop elasticsearch.service

To get the fingerprint 

cd /etc/elasticsearch/certs
openssl x509 -fingerprint -sha256 -in http_ca.crt

=============================

When first installed keep this info handy for config

Elasticsearch security features have been automatically configured!
Authentication is enabled and cluster connections are encrypted.

Password for the elastic user (reset with `bin/elasticsearch-reset-password -u elastic`):
kmfB2C9v5vP95BF-kKxX

HTTP CA certificate SHA-256 fingerprint:
  5f7269520e0f1cbb8c551c3e612dd9856339aa0fbbff70c12ab3e8ee27937810

Configure Kibana to use this cluster:

Run Kibana and click the configuration link in the terminal when Kibana starts.
Copy the following enrollment token and paste it into Kibana in your browser (valid for the next 30 minutes):
  eyJ2ZXIiOiI4LjYuMSIsImFkciI6WyIxOTIuMTY4LjEwOS4xMjk6OTIwMCJdLCJmZ3IiOiI1ZjcyNjk1MjBlMGYxY2JiOGM1NTFjM2U2MTJkZDk4NTYzMzlhYTBmYmJmZjcwYzEyYWIzZThlZTI3OTM3ODEwIiwia2V5IjoibkY0NVM0WUJraGR4bE5oNG5OSjM6VWp0djZxNlBSZC1yOVVla2Q5cW03dyJ9

Configure other nodes to join this cluster:

On this node:
  Create an enrollment token with `bin/elasticsearch-create-enrollment-token -s node`.
  Uncomment the transport.host setting at the end of config/elasticsearch.yml.
  Restart Elasticsearch.
On other nodes:
  Start Elasticsearch with `bin/elasticsearch --enrollment-token <token>`, using the enrollment token that you generated.
  
On windows you will need to install  the root cert 

Import certificate from config\certs http_ca.crt as Trusted Root Certification (windows) 


If you need a dashboard you will need to also install kibana

https://www.elastic.co/downloads/kibana

