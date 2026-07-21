from fastapi import FastAPI
from openap import prop
from openap.kinematic import WRAP
from pydantic import BaseModel
from openap import FuelFlow

app = FastAPI()

class AircraftRequest(BaseModel):
    aircraft_type: str

class FuelFlowParameters(BaseModel):
    mass: int
    tas: int
    alt: int
    vs: int
    acc: int
    dT: int
    aircraft_type: str

@app.get("/")
def root():
    return "PDS ONLINE"

@app.post("/get_aircraft_info")
def get_aircraft_info(request: AircraftRequest):
    return prop.aircraft(request.aircraft_type)

@app.post("/get_aircraft_fuel_capacity")
def get_aircraft_fuel_capacity(request: AircraftRequest):
    aircraft_info = get_aircraft_info(request)
    return aircraft_info["limits"]["MFC"]

@app.post("/get_aircraft_passenger_load_range")
def get_aircraft_passenger_load_range(request: AircraftRequest):
    aircraft_info = get_aircraft_info(request)
    return aircraft_info["pax"]

@app.post("/get_aircraft_oew")
def get_aircraft_oew(request: AircraftRequest):
    aircraft_info = get_aircraft_info(request)
    return aircraft_info["limits"]["OEW"]

@app.post("/get_climb_init_vcas")
def get_climb_init_vcas(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.initclimb_vcas()["default"]

@app.post("/get_climb_const_vcas")
def get_climb_const_vcas(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_const_vcas()["default"]

@app.post("/get_climb_const_mach")
def get_climb_const_mach(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_const_mach()["default"]

@app.post("/get_climb_concas_xover_alt")
def get_climb_concas_xover_alt(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_cross_alt_concas()["default"]

@app.post("/get_climb_conmach_xover_alt")
def get_climb_conmach_xover_alt(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_cross_alt_conmach()["default"]

@app.post("/get_initclimb_vs")
def get_wrap_initclimb_vs_mean(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.initclimb_vs()
    return params["default"]

@app.post("/get_climb_vs_const_cas")
def get_climb_vs_const_cas(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_concas()
    return params["default"]

@app.post("/get_climb_vs_const_mach")
def get_climb_vs_const_mach(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_conmach()
    return params["default"]

@app.post("/get_enroute_fuelflow")
def get_enroute_fuelflow(ff_params: FuelFlowParameters):
    fuel_flow = FuelFlow(ff_params.aircraft_type)
    flow = fuel_flow.enroute(
        mass=ff_params.mass,
        tas=ff_params.tas,
        alt=ff_params.alt,
        vs=ff_params.vs,
        acc=ff_params.acc,
        dT=ff_params.dT
    )

    return flow