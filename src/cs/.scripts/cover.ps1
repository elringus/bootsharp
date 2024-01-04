try {
	dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:ExcludeByAttribute=GeneratedCodeAttribute
	reportgenerator "-reports:*/*.xml" "-targetdir:.cover" -reporttypes:HTML
	serve .cover
	python -m webbrowser http://localhost:3000
} finally {
	rm ./*/coverage.opencover.xml -force
	rm ./.cover -r -force
}
