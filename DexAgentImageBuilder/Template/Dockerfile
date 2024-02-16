# Use the windows image which is based on Windows
FROM mcr.microsoft.com/windows/server:ltsc2022

# Set the working directory
WORKDIR /app

# Copy files to the working directory
COPY . .

# Set an environment variable for the Tricentis Tosca installation path
ENV SETUP_PATH="C:\\Program Files (x86)\\TRICENTIS\\Tosca Testsuite\\Licensing"

# Use PowerShell to execute the command

RUN Setup.exe /s MOBILE_TESTING=1 EXAMPLE_WORKSPACES=1 ADDLOCAL=TricentisTBox,DexAgent /qn 

RUN xcopy /Y ".\ToscaDistributionAgent.exe.config" "C:\\Program Files (x86)\\TRICENTIS\\Tosca Testsuite\\DistributedExecution"

WORKDIR ${SETUP_PATH}

RUN ToscaLicenseConfiguration.exe connect-on-premise -a licsrv -o 7070


# Set the working directory in the container
WORKDIR "C:\\Program Files (x86)\\TRICENTIS\\Tosca Testsuite\\DistributedExecution"

# Specify the command to run your application
EntryPoint ["ToscaDistributionAgent.exe"]

