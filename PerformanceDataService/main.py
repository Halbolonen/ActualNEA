from fastapi import FastAPI

app = FastAPI()
items = ["B738","A320","B77W","B752"]

@app.get("/")
def root():
    return items

@app.post("/items")
def create_item(item: str):
    items.append(item)
    return item

@app.get("/items/{item_id}")
def get_item(item_id: int) -> str:
    item = items[item_id]
    return item

@app.post("/remove")
def remove_items(removal_list: list[str]) -> list[str]:
    for item in removal_list:
        while item in items:
            items.remove(item)
    return items


# use Invoke-RestMethod to call these things from PS.
    """
        example commands:
        PS C:\WINDOWS\system32> Invoke-RestMethod -Method 'Post' -Uri http://localhost:8000/remove -ContentType "application/json" -Body '["B738","A320"]'
        PS C:\WINDOWS\system32> Invoke-RestMethod -Method 'Post' -Uri http://localhost:8000/items?item=B752

    """

# use uvicorn main:app --reload to run the API endpoint. make sure you are in the directory of main and in the venv when you do that.
# enter venv using .\venv\Scripts\activate