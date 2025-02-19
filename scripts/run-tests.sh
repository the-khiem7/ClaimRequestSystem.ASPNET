#!/bin/bash

# Remove existing containers and images
docker-compose down
docker rmi claimrequesttest

# Build and run tests
docker-compose build --no-cache claimrequest.test
docker-compose run --rm claimrequest.test 