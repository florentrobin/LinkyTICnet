﻿#!/bin/bash

dotnet publish -c Release -r linux-arm -p:PublishSingleFile=true --self-contained true