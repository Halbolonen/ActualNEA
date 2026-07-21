import OpenAP_API as api
from fastapi import FastAPI
from pydantic import BaseModel
import math
from numpy import clip
from enum import Enum

performance_calculator = FastAPI()

class AircraftRequest(BaseModel):
    aircraft_type: str

class FlightRequest(BaseModel):
    departure_arprt_alt: int
    arrival_arprt_alt: int
    acft_request: AircraftRequest
    zfw: int
    cruise_altitude: int

class FuelFlowParameters(BaseModel):
    mass: int
    tas: float
    alt: float
    vs: float
    acc: int
    dT: int
    aircraft_type: str

class FlightPhase(Enum):
    TAKEOFF = 0
    CLIMB = 1
    CRUISE = 2
    DESCENT = 3
    FINAL_APPROACH = 4
    DESCENT_TRACK_LENGTH_COMPUTATION = 5

@performance_calculator.get("/")
def root():
    return "PDS PERFORMANCE CALCULATOR ONLINE"

def cas_to_tas(altitude: float, cas: float):
    # https://www.grc.nasa.gov/www/k-12/airplane/atmosmet.html

    t_0: float = 288.15
    # ISA standard temperature at sea level, in kelvin.
    p_0: float = 101.325
    # ISA standard pressure at sea level, in kilopascals.
    R: float = 287
    # Gas constant for air, in m^2/s^2/K.
    gamma: float = 1.40
    # specific heat ratio for calorically perfect air.
    a_0: float = 340.294
    # speed of sound at sea level, m/s.
    ISA_LAPSE_RATE = 6.5
    # loss of air temperature in centigrade per km.

    t: float = t_0 - (altitude / 1000) * ISA_LAPSE_RATE
    localSpeedOfSound = math.sqrt(gamma * R * t)
    # https://www.grc.nasa.gov/www/k-12/VirtualAero/BottleRocket/airplane/sound.html

    P: float = p_0 * math.pow(t / t_0, 5.256)
    # TODO: label magic number -5.256
    # static pressure at altitude

    qc: float = p_0 * (math.pow(1 + 0.2 * math.pow(cas / a_0, 2), 3.5) - 1)
    # subsonic impact pressure, mach

    machNumber: float = math.sqrt(5 * (math.pow(qc / P + 1, 1 / 3.5) - 1))
    # subsonic mach number
    # https://www.aviationhunt.com/airspeed-conversion-calculator/

    return machNumber * localSpeedOfSound

def mach_to_tas(altitude: float, machNumber: float):
    # https://www.grc.nasa.gov/www/k-12/airplane/atmosmet.html

    gamma: float = 1.40
    # specific heat ratio for calorically perfect air.
    R: float = 287
    # Gas constant for air, in m^2/s^2/K.
    t_0: float = 288.15
    # ISA standard temperature at sea level, in kelvin.
    ISA_LAPSE_RATE = 6.5
    # loss of air temperature in centigrade per km.
    t: float = t_0 - (altitude / 1000) * ISA_LAPSE_RATE

    localSpeedOfSound = math.sqrt(gamma * R * t)
    # https://www.grc.nasa.gov/www/k-12/VirtualAero/BottleRocket/airplane/sound.html

    return machNumber * localSpeedOfSound

def compute_gross_mass(fuel_mass: float, max_fuel_capacity: int, zfw: int):
    fuel_mass_int: int = round(clip(fuel_mass, 0, max_fuel_capacity))
    return fuel_mass_int + zfw

def get_fuel_flow(ff_params: FuelFlowParameters):
    return api.get_fuelflow(ff_params)

@performance_calculator.post("/get_flight_fuel_consumption")
def get_flight_fuel_consumption(flight_request: FlightRequest):
    M_TO_NMI: float = 0.000539957
    dt: int = 10
    cas: int
    mach: int
    descent_mach: int
    distance_travelled: float
    descent_track_length: float
    climb_and_cruise_track_length: float
    block_fuel_estimate: int = api.get_aircraft_fuel_capacity(flight_request.acft_request) / 2
    remaining_fuel: float = block_fuel_estimate
    descent_ff_params: FuelFlowParameters = FuelFlowParameters()
    phase_of_flight: FlightPhase = FlightPhase.CLIMB
    cas = api.get_initclimb_vs(flight_request.acft_request)
    max_fuel_capacity = api.get_aircraft_fuel_capacity(flight_request.acft_request)

    constant_cas_climb_vs = api.get_climb_vs_const_cas(flight_request.acft_request)
    constant_mach_climb_vs = api.get_climb_vs_const_mach(flight_request.acft_request)
    constant_climb_mach = api.get_climb_const_mach(flight_request.acft_request)
    descent_const_mach = api.get_descent_const_mach(flight_request.acft_request)
    descent_const_cas = api.get_descent_const_vcas(flight_request.acft_request)
    descent_const_mach_vs = api.get_descent_vs_const_mach(flight_request.acft_request)
    descent_xover_alt_const_mach = api.get_descent_xover_alt_const_mach(flight_request.acft_request)
    descent_xover_alt_const_cas = api.get_descent_vs_const_cas(flight_request.acft_request)
    descent_const_cas_vs = api.get_descent_vs_const_cas(flight_request.acft_request)
    finalapproach_cas = api.get_finalapp_vcas(flight_request.acft_request)
    finalapproach_vs = api.get_finalapp_vs(flight_request.acft_request)
    cruise_mach = api.get_cruise_mach(flight_request.acft_request)

    ff_params: FuelFlowParameters = FuelFlowParameters()
    ff_params.alt = flight_request.departure_arprt_alt
    ff_params.vs = api.get_initclimb_vs()
    ff_params.mass = compute_gross_mass(remaining_fuel, max_fuel_capacity, flight_request.zfw)
    ff_params.aircraft_type = flight_request.acft_request.aircraft_type

    def run_simulation_tick():
        flow: float
        angle_of_climb: float

        match phase_of_flight:
            case FlightPhase.DESCENT_TRACK_LENGTH_COMPUTATION:
                angle_of_climb = math.asin(descent_ff_params.vs / descent_ff_params.tas)
                descent_track_length += descent_ff_params.tas * dt * math.cos(angle_of_climb) * M_TO_NMI
                descent_ff_params.alt += descent_ff_params.vs * dt
                return
            case _:
                angle_of_climb = math.asin(ff_params.vs / ff_params.tas)
                flow = api.get_fuelflow(ff_params)

                remaining_fuel -= flow * dt
                ff_params.alt += ff_params.vs * dt
                ff_params.mass = compute_gross_mass(remaining_fuel, max_fuel_capacity, flight_request.zfw)
                distance_travelled += ff_params.tas * dt * math.cos(angle_of_climb) * M_TO_NMI
                return
            
    # initial vertical speed & cas

    while (ff_params.alt < constant_cas_climb_vs):
        ff_params.tas = round(cas_to_tas(ff_params.alt, cas))
        run_simulation_tick()
    
    # entering constant mach climb
    ff_params.vs = constant_mach_climb_vs
    mach = constant_climb_mach

    while (ff_params.alt < flight_request.cruise_altitude):
        ff_params.tas = round(mach_to_tas(ff_params.alt, mach))
        run_simulation_tick()
    
    # getting descent track length before simulating cruise
    phase_of_flight = FlightPhase.DESCENT_TRACK_LENGTH_COMPUTATION
    descent_ff_params = ff_params
    # entering constant mach stage of descent
    mach = descent_const_mach
    descent_ff_params.vs = descent_const_mach_vs
    while (descent_ff_params.alt > descent_xover_alt_const_cas):
        descent_ff_params.tas = round(cas_to_tas(descent_ff_params.alt, cas))
        run_simulation_tick()
    
    # entering constant CAS stage of descent
    cas = descent_const_cas
    descent_ff_params.vs = descent_const_cas_vs
    while (descent_ff_params.alt > descent_xover_alt_const_cas):
        descent_ff_params.tas = round(cas_to_tas(descent_ff_params.alt, cas))
        run_simulation_tick()

    # entering final approach stage of descent
    cas = finalapproach_cas
    descent_ff_params.vs = finalapproach_vs
    while (descent_ff_params.alt > flight_request.arrival_arprt_alt):
        ff_params.tas = round(cas_to_tas(descent_ff_params.alt, cas))
        run_simulation_tick()

    # entering cruise
    ff_params.vs = 0
    mach = cruise_mach
    phase_of_flight = FlightPhase.CRUISE
    ff_params.alt = flight_request.cruise_altitude
    ff_params.tas = round(mach_to_tas(ff_params.alt, mach))

    while (distance_travelled < climb_and_cruise_track_length):
        run_simulation_tick();

    # simulating descent stages for real this time
    phase_of_flight = FlightPhase.DESCENT
    # entering constant mach stage of descent
    mach = descent_const_mach
    ff_params.vs = descent_const_mach_vs
    while (ff_params.alt > descent_xover_alt_const_cas):
        ff_params.tas = round(cas_to_tas(ff_params.alt, cas))
        run_simulation_tick()
    
    # entering constant CAS stage of descent
    cas = descent_const_cas
    ff_params.vs = descent_const_cas_vs
    while (ff_params.alt > descent_xover_alt_const_cas):
        ff_params.tas = round(cas_to_tas(ff_params.alt, cas))
        run_simulation_tick()

    # entering final approach stage of descent
    cas = finalapproach_cas
    ff_params.vs = finalapproach_vs
    while (ff_params.alt > flight_request.arrival_arprt_alt):
        ff_params.tas = round(cas_to_tas(ff_params.alt, cas))
        run_simulation_tick()

    remaining_fuel = round(remaining_fuel)

    return remaining_fuel

    # TODO: use newton-raphson to make this find an optimal fuel load
    # (or error correction if newton-raphson turns out to be too complex)