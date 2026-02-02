import numpy as np
from gymnasium import spaces

# note that this is PoolEnvMini, the name is a convention
class PoolEnv:

    # envType = 0 -> CustomEnv.Normal
    # envType = 1 -> CustomEnv.OneBallShot

    def __init__(self, env_wrapper, envType=0):

        self.wrapper = env_wrapper # global from C#
        self.wrapper.EnvType = envType # 0 = Normal, 1 = OneBall

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

    # (state, info)
    def reset(self, seed=None):
        if seed is None:
            self.wrapper.Reset(-1) # uses default 4477
        else:
            self.wrapper.Reset(seed)
        
        return self.getState(), {}
    
    # StepResult, action is float[] but only arg0 is used
    def step(self, action):
        result = self.wrapper.Step(float(action[0]))
        
        obs = np.array(result.State, dtype=np.float32) # CustomGetState is used if not null
        reward = float(result.Reward) # CustomGetReward is used if not null
        done = bool(result.Done)

        # should be done automatically
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
    
    # void
    def setAction(self, angle, strength=1500.0):
        self.wrapper.SetAction(angle, strength)
    
    # float[]
    def getState(self):
        return np.array(self.wrapper.GetState(), dtype=np.float32)
    
    # float
    def getReward(self, ballsFell, ending):
        return float(self.wrapper.GetReward(ballsFell, ending))