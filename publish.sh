#!/usr/bin/env bash

SCRIPT_PATH="$(cd -- "$(dirname "$0")" 2>&1 > /dev/null; pwd -P)"

RED=$(tput setaf 1)
BLUE=$(tput setaf 4)
BOLD=$(tput bold)
RESET=$(tput sgr0)

# --- Script options parsing ---
CONFIGURATION="DEBUG"
while [[ $# -gt 0 ]]; do
	case "$1" in
		--release|-r)
			CONFIGURATION="RELEASE"
			shift
			;;
		
		--debug|-d)
			CONFIGURATION="DEBUG"
			shift
			;;
		
		*)
		  echo -e "${RED}■ Unknown option : '$1' ${RESET}"
		  echo -e "Usage: $0 [options]\n"
		  echo -e "Options:"
		  echo -e " --release|-r \t Build package(s) within .nupkg format and matching symbols within .snupkg format"
		  echo -e " --debug|-d \t Build package(s) within .nupkg format while embedding symbols inside, for debugging purpose"
		  exit 1
		  ;;
  esac
done

BUILD_FILE="$SCRIPT_PATH/Directory.Build.props"
PUBLISH_CONFIG_FILE="$SCRIPT_PATH/publish.config"
PUBLISH_LOG_FILE="$SCRIPT_PATH/publish.log"
PUBLISH_FOLDER="$SCRIPT_PATH/publish"

echo -e "► Navigating to '$SCRIPT_PATH' \n"
cd "$SCRIPT_PATH"

# --- Sanity check ---

if [[ ! -r $PUBLISH_CONFIG_FILE ]]; then
	echo -e "${RED}■ Unable to read config file '$PUBLISH_CONFIG_FILE', make sure it exists and is readable ${RESET}" >&2
	read -n 1 -r -t 2
	exit 1
fi

if [[ ! -r $BUILD_FILE ]]; then
	echo -e "${RED}■ Unable to read build file '$BUILD_FILE', make sure it exists and is readable ${RESET}" >&2
	read -n 1 -r -t 2
	exit 1
fi

if [[ ! -x "$(command -v dotnet)" ]]; then
	echo -e "${RED}■ Unable to find 'dotnet' command, make sure you have everything installed and set up in PATH ${RESET}" >&2
	read -n 1 -r -t 2
	exit 1
fi

if [[ ! -x "$(command -v git)" ]]; then
	echo -e "${RED}■ Unable to find 'git' command, make sure you have everything installed and set up in PATH ${RESET}" >&2
	read -n 1 -r -t 2
	exit 1
fi

spinner() {
	pid=$!
	text=${1:-"Please wait"}
	spin='-\|/'
	i=0
	while kill -0 $pid 2>/dev/null
	do
	  i=$(( (i+1) %4 ))
	  printf "\r[${spin:$i:1}] $text"
	  sleep .3
	done
	
	echo -e "${RESET}\n"
}

source $PUBLISH_CONFIG_FILE

echo -e "► Displaying content of '$PUBLISH_CONFIG_FILE'"

cat $PUBLISH_CONFIG_FILE
echo -e "${RESET}\n"

read -p "${BLUE}► Are you sure those settings are correct (y/n)?${RESET} > " -r -t 60 REPLY 
if [[ ! $REPLY =~ ^[Yy](es)?$ ]]; then
	echo -e "\n${RED}■ Aborting ${RESET}" >&2
	read -n 1 -r -t 2
	exit 1
fi
echo

BUILD_START_TIME=`date +%s`

# --- Cleanup to avoid useless artifacts ---
echo -e "► Cleaning artifacts"

rm -rf $PUBLISH_LOG_FILE $PUBLISH_FOLDER 2>&1 > /dev/null
(rm -rf **/bin **/obj 2>&1 > $PUBLISH_LOG_FILE && sleep 1)&
spinner "Cleaning bin/obj from solution, it may take few seconds"

# --- Build solution to be able to pack it ---
echo -e "► Pre-building solution"
(dotnet build --configuration Release 2>&1 > $PUBLISH_LOG_FILE || echo -e "${RED}■ Fatal error while pre-building, find details in '$PUBLISH_LOG_FILE' ${RESET}" >&2 && exit 1)&
spinner "Pre-building solution, it may take a while"

# --- Nuget build task ---
if [[ $CONFIGURATION = "DEBUG" ]]; then

	echo -e "► Building BLEEDING nuget package"
	
	VERSION_PREFIX=$(grep -oPm1 "(?<=<VersionPrefix>)[^<]+" Directory.Build.props)
	VERSION_SUFFIX="-build.$(date '+%Y%m%d%H%M%S')+$(git log -1 --format='%h')"
	
	read -p "${BLUE}► You're about to build version '$VERSION_PREFIX$VERSION_SUFFIX' from 'Directory.Build.props', are you sure it's correct (y/n)?${RESET} > " -r -t 30
	if [[ ! $REPLY =~ ^[yY](es)?$ ]]; then
		echo -e "\n${RED}■ Aborting ${RESET}" >&2
		read -n 1 -r -t 2
		exit 1
	fi
	echo
		
	(dotnet pack --configuration Release --output "$PUBLISH_FOLDER" -p:DebugType=embedded -p:VersionSuffix="$VERSION_SUFFIX" 2>&1 > $PUBLISH_LOG_FILE || echo -e "${RED}■ Fatal error while packing, find details in '$PUBLISH_LOG_FILE' ${RESET}" >&2 && exit 1)&
	spinner "Building DEBUG nuget package, it may take a while"
else

	echo -e "► Building RELEASE nuget package"
	
	VERSION_PREFIX=$(grep -oPm1 "(?<=<VersionPrefix>)[^<]+" Directory.Build.props)
	VERSION_SUFFIX=$(grep -oPm1 "(?<=<VersionSuffix>)[^<]+" Directory.Build.props)
	
	read -p "${BLUE}► You're about to build version '$VERSION_PREFIX$VERSION_SUFFIX' from 'Directory.Build.props', are you sure it's correct (y/n)?${RESET} > " -r -t 30 REPLY
	if [[ ! $REPLY =~ ^[yY](es)?$ ]]; then
		echo -e "\n${RED}■ Aborting ${RESET}" >&2
		read -n 1 -r -t 2
		exit 1
	fi
	echo

	(dotnet pack --configuration Release --output "$PUBLISH_FOLDER" --include-symbols -p:SymbolPackageFormat=snupkg -p:IncludeSymbols=true 2>&1 > $PUBLISH_LOG_FILE || echo -e "\n${RED}■ Fatal error while packing, find details in '$PUBLISH_LOG_FILE' ${RESET}" >&2 && exit 1)&
	spinner "Building nuget package, it may take a while"
fi

cd publish

echo -e "► Built nuget packages :"
find . -name '*.nupkg' -print
echo -e "${RESET}"

read -p "${BLUE}► Are you sure you want to publish to nuget feed (y/n)?${RESET} > " -r -t 60 REPLY
if [[ ! $REPLY =~ ^[Yy](es)?$ ]]; then
	echo -e "\n${RED}■ Aborting ${RESET}" >&2
	read -n 1 -r -t 2
    exit 1
fi
echo

# --- Nuget push task ---

(dotnet nuget push *.nupkg --api-key "$NUGET_API_KEY" --source "$NUGET_FEED_URL" --skip-duplicate --timeout 40 --interactive 2>&1 > $PUBLISH_LOG_FILE || echo -e "\n${RED}■ Fatal error while pushing, find details in '$PUBLISH_LOG_FILE' ${RESET}" >&2 && exit 1)&
spinner "Pushing nuget packages"

BUILD_END_TIME=`date +%s`

echo "► Executed in $(($BUILD_END_TIME - $BUILD_START_TIME))s"