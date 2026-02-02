import numpy as np
from env import PoolEnv

if 'env_wrapper' not in globals():
    raise ImportError("env_wrapper is not defined")

env = PoolEnv(globals()['env_wrapper'], envType=1) # 4 inputs

print(env.reset())
# obs, reward, done, truncated, info = env.step([0])

# print(obs)
# print(reward, done, truncated, info)