from core import env_wrapper # from c#
import numpy as np
import gymnasium as gym
from gymnasium import spaces

# note that this is PoolEnvMini, the name is a convention
class PoolEnv(gym.Env):
    metadata = {'render.modes': []}

    # envType = 0 -> CustomEnv.Normal
    # envType = 1 -> CustomEnv.OneBallShot

    def __init__(self, envType=0):
        super().__init__()

        # NOTE: 'env_wrapper', 'printc' and other globals are only globally defined in core.py from C#
        # so 'core' must be imported to use globals()['env_wrapper'] and such
        self.wrapper = env_wrapper # global from C#
        self.wrapper.EnvType = envType

        inputs = 47 if envType == 0 else 4

        self.observation_space = spaces.Box(
            low=-np.inf,
            high=np.inf,
            shape=(inputs,),
            dtype=np.float32
        )
        self.action_space = spaces.Box(
            low=-1.0,
            high=1.0,
            shape=(1,),
            dtype=np.float32
        )

        # gym automatically calls reset

    # (state, info)
    def reset(self, seed=None, options=None):
        super().reset(seed=seed)

        if seed is None:
            self.wrapper.Reset(-1) # uses default 4477
        else:
            self.wrapper.Reset(seed)

        return self.getState(), {}
    
    # StepResult, action is float[] but only arg0 is used
    def step(self, action):
        action = np.clip(action, -1.0, 1.0) # not sure if needed

        result = self.wrapper.Step(float(action[0]))
        
        obs = np.array(result.State, dtype=np.float32) # CustomGetState is used if not null
        reward = float(result.Reward) # CustomGetReward is used if not null
        done = bool(result.Done)

        # is done automatically by gym (NOT C#)
        # if done:
        #     obs, _ = self.reset()
        
        info = {
            "balls_fell": int(result.BallsFell),
            "ticks": int(result.Ticks),
            "ending": int(result.Ending) # enum (0 = Running, 1 = Victory, 2 = Loss) in IPoolEnv.cs
        }

        # gym styled
        return obs, reward, done, False, info
    
    def render(self): pass
    def close(self): pass
    
    # void, debugging purposes
    def setAction(self, angle, strength=1500.0):
        self.wrapper.SetAction(angle, strength)
    
    # float[]
    def getState(self):
        return np.array(self.wrapper.GetState(), dtype=np.float32)
    
    # float, debugging purposes
    def getReward(self, ballsFell, ending):
        return float(self.wrapper.GetReward(ballsFell, ending))