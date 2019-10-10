# Explore Background Worker

Just a simple background worker and web application that implement `IHostedService`, to check shutdown handling.

Check logs:

- For a simple background worker, check D:\home\LogFiles\BackgroundWorker-YYYYMMDD.log
- For a simple web app, check D:\home\LogFiles\WebApplication-YYYYMMDD.log

To force an AppPool recycle, just edit the web.config (as suggested in [this SO issue](https://stackoverflow.com/questions/48486369/is-it-possible-to-recycle-app-pool-for-a-azure-web-app))

