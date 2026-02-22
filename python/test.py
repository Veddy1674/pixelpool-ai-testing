from core import printc
from env import PoolEnv

env = PoolEnv(envType=1) # 4 inputs
env.reset()

action = env.action_space.sample()
obs, reward, done, truncated, info = env.step(action)

printc(f"&dAction: {action}")
printc(f"&dState: {[f'{x:.3f}' for x in obs]}") # format each number to 3 decimals
printc(f"&dReward: {reward}")
printc(f"&cDone: {done}")
printc(f"&cTruncated: {truncated}")
printc(f"&dInfo: {info}")
printc(f"\nArguments passed:")
printc(f"Arg0: {globals()['arg0'] if 'arg0' in globals() else 'null'}")
printc(f"Arg1: {globals()['arg1'] if 'arg1' in globals() else 'null'}")
printc(f"Arg2: {globals()['arg2'] if 'arg2' in globals() else 'null'}")