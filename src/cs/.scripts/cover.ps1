try {
	dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
	reportgenerator "-reports:*/*.xml" "-targetdir:.cover" -reporttypes:HTML
	serve .cover
} finally {
	rm ./*/coverage.opencover.xml -force
	rm ./.cover -r -force
}
