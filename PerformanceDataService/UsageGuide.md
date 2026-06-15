use Invoke-RestMethod to call these things from PS.<br><br>
        example commands:<br>
        `PS C:\WINDOWS\system32> Invoke-RestMethod -Method 'Post' -Uri http://localhost:8000/remove -ContentType "application/json" -Body '["B738","A320"]'`<br>
        `PS C:\WINDOWS\system32> Invoke-RestMethod -Method 'Post' -Uri http://localhost:8000/items?item=B752<br>`<br><br>
        linux/curl:<br>
        `curl -X POST -H 'application/json' http://localhost:8000/items?item=B762`<br><br>
use uvicorn main:app --reload to run the API endpoint. make sure you are in the directory of main and in the venv when you do that.<br>
enter venv using .\venv\Scripts\activate
