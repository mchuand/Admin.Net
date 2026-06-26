@echo off
CHCP 65001

set dir=%~dp0

set moduleName=apiServices
set apiServicesPath=%dir%..\src\api-services\
set apiUrl=http://localhost:5005/swagger/Default/swagger.json

if "%1"=="approvalFlow" (
  set moduleName=approvalFlow
  set apiServicesPath=%dir%..\src\api-plugins\approvalFlow\
  set apiUrl=http://localhost:5005/swagger/ApprovalFlow/swagger.json
) else if "%1"=="dingTalk" (
  set moduleName=dingTalk
  set apiServicesPath=%dir%..\src\api-plugins\dingTalk\
  set apiUrl=http://localhost:5005/swagger/DingTalk/swagger.json
) else if "%1"=="goView" (
  set moduleName=goView
  set apiServicesPath=%dir%..\src\api-plugins\goView\
  set apiUrl=http://localhost:5005/swagger/DingTalk/swagger.json
)

if exist %apiServicesPath% (
    echo ================================ ???? %moduleName% ================================
    rd /s /q %apiServicesPath%
)

echo ================================ ???? %moduleName% ================================

java -jar %dir%swagger-codegen-cli.jar generate -i %apiUrl% -l typescript-axios -o %apiServicesPath%

@rem ????????????
rd /s /q %apiServicesPath%.swagger-codegen
del /q %apiServicesPath%.gitignore
del /q %apiServicesPath%.npmignore
del /q %apiServicesPath%.swagger-codegen-ignore
del /q %apiServicesPath%git_push.sh
del /q %apiServicesPath%package.json
del /q %apiServicesPath%README.md
del /q %apiServicesPath%tsconfig.json

echo ================================ ???? ================================