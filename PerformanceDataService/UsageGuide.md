# use Invoke-RestMethod to call these things from PS.
        example commands:
        PS C:\WINDOWS\system32> Invoke-RestMethod -Method 'Post' -Uri http://localhost:8000/remove -ContentType "application/json" -Body '["B738","A320"]'
        PS C:\WINDOWS\system32> Invoke-RestMethod -Method 'Post' -Uri http://localhost:8000/items?item=B752

# use uvicorn main:app --reload to run the API endpoint. make sure you are in the directory of main and in the venv when you do that.
# enter venv using .\venv\Scripts\activate