#!/usr/bin/env bash

docker build \
	--tag 6opuc/lldb-netcore-use-cases:2.2.3 \
	--network host \
	--build-arg http_proxy=http://127.0.0.1:3128 \
   	--build-arg https_proxy=http://127.0.0.1:3128 \
	.
