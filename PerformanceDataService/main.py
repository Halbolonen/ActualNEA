from fastapi import FastAPI
from openap import prop
from openap.kinematic import WRAP
from pydantic import BaseModel

app = FastAPI()

class AircraftRequest(BaseModel):
    aircraft_type: str

@app.get("/")
def root():
    return "PDS ONLINE"

@app.post("/get_aircraft_info")
def get_aircraft_info(request: AircraftRequest):
    return prop.aircraft(request.aircraft_type)

@app.post("/get_wrap_initclimb_vs_mean")
def get_wrap_initclimb_vs_mean(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.initclimb_vs()
    return params["default"]

@app.post("/get_wrap_cruise_alt")
def get_wrap_cruise_alt(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.cruise_alt()
    return params["default"]

@app.post("/get_wrap_climb_const_vcas_mean")
def get_wrap_climb_const_vcas_mean(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_const_vcas()
    return params["default"]
    
@app.post("/get_wrap_climb_vs_pre_concas_mean")
def get_wrap_climb_vs_pre_concas_mean(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_pre_concas()
    return params["default"]

@app.post("/get_wrap_climb_vs_concas_mean")
def get_wrap_climb_vs_concas_mean(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_concas()
    return params["default"]

@app.post("/get_wrap_climb_vs_conmach_mean")
def get_wrap_climb_vs_conmach_mean(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_conmach()
    return params["default"]