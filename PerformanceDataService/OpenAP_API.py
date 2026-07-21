from fastapi import FastAPI
from openap import prop, FuelFlow, Thrust, Drag
from openap.kinematic import WRAP
from pydantic import BaseModel
import math

api = FastAPI()

class AircraftRequest(BaseModel):
    aircraft_type: str

class FuelFlowParameters(BaseModel):
    mass: int
    tas: float
    alt: float
    vs: float
    acc: int
    dT: int
    aircraft_type: str

@api.get("/")
def root():
    return "PDS API ONLINE"

@api.post("/get_aircraft_info")
def get_aircraft_info(request: AircraftRequest):
    return prop.aircraft(request.aircraft_type)

@api.post("/get_aircraft_fuel_capacity")
def get_aircraft_fuel_capacity(request: AircraftRequest):
    aircraft_info = get_aircraft_info(request)
    return aircraft_info["limits"]["MFC"]

@api.post("/get_aircraft_passenger_load_range")
def get_aircraft_passenger_load_range(request: AircraftRequest):
    aircraft_info = get_aircraft_info(request)
    return aircraft_info["pax"]

@api.post("/get_aircraft_oew")
def get_aircraft_oew(request: AircraftRequest):
    aircraft_info = get_aircraft_info(request)
    return aircraft_info["limits"]["OEW"]

@api.post("/get_climb_init_vcas")
def get_climb_init_vcas(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.initclimb_vcas()["default"]

@api.post("/get_climb_const_vcas")
def get_climb_const_vcas(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_const_vcas()["default"]

@api.post("/get_climb_const_mach")
def get_climb_const_mach(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_const_mach()["default"]

@api.post("/get_climb_concas_xover_alt")
def get_climb_concas_xover_alt(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_cross_alt_concas()["default"]

@api.post("/get_climb_conmach_xover_alt")
def get_climb_conmach_xover_alt(request: AircraftRequest):
    wrap = WRAP(request.aircraft_type)
    return wrap.climb_cross_alt_conmach()["default"]

@api.post("/get_initclimb_vs")
def get_initclimb_vs(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.initclimb_vs()
    return params["default"]

@api.post("/get_climb_vs_const_cas")
def get_climb_vs_const_cas(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_concas()
    return params["default"]

@api.post("/get_climb_vs_const_mach")
def get_climb_vs_const_mach(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.climb_vs_conmach()
    return params["default"]

@api.post("/get_cruise_mach")
def get_cruise_mach(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.cruise_mach()
    return params["default"]

@api.post("/get_descent_const_mach")
def get_descent_const_mach(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.descent_const_mach()
    return params["default"]

@api.post("/get_descent_const_vcas")
def get_descent_const_vcas(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.descent_const_vcas()
    return params["default"]

@api.post("/get_descent_vs_const_mach")
def get_descent_vs_const_mach(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.descent_vs_conmach()
    return params["default"]

@api.post("/get_descent_vs_const_cas")
def get_descent_vs_const_cas(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.descent_vs_concas()
    return params["default"]

@api.post("/get_descent_xover_alt_const_mach")
def get_descent_xover_alt_const_mach(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.descent_cross_alt_conmach()
    return params["default"]

@api.post("/get_descent_xover_alt_const_cas")
def get_descent_xover_alt_const_cas(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.descent_cross_alt_concas()
    return params["default"]

@api.post("/get_finalapp_vcas")
def get_finalapp_vcas(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.finalapp_vcas()
    return params["default"]

@api.post("/get_finalapp_vs")
def get_finalapp_vs(request: AircraftRequest):
    wrap_model = WRAP(ac=request.aircraft_type)
    params = wrap_model.finalapp_vs()
    return params["default"]

@api.post("/get_enroute_fuelflow")
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
    print(ff_params.mass, ff_params.alt, flow)

    return flow

@api.post("/get_fuelflow")
def get_fuelflow(ff_params: FuelFlowParameters):
    g = 9.80665
    MpS_TO_KTS = 1.94384
    M_TO_FT = 3.28084
    MpS_TO_FpM = 196.85

    angle_of_climb = math.asin(ff_params.vs / ff_params.tas)

    # converting units after angle calc for drag model

    ff_params.tas = ff_params.tas * MpS_TO_KTS
    ff_params.alt = ff_params.alt * M_TO_FT
    ff_params.vs = ff_params.vs * MpS_TO_FpM
    drag_model = Drag(ff_params.aircraft_type)
    climb_drag = drag_model.clean(ff_params.mass, ff_params.tas, ff_params.alt, ff_params.vs)
    weight_component_against_thrust = ff_params.mass * g * math.sin(angle_of_climb)
    climb_thrust = climb_drag + weight_component_against_thrust

    fuelflow_model = FuelFlow(ff_params.aircraft_type)
    flow = fuelflow_model.at_thrust(
        total_ac_thrust=climb_thrust
    )

    return flow