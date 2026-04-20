using UnityEngine;

namespace DontDillyDally.Data
{
    public sealed class MachineParticleController
    {
        private readonly ParticleSystem _workingParticle;
        private readonly ParticleSystem _openedParticle;

        public MachineParticleController(ParticleSystem workingParticle, ParticleSystem openedParticle)
        {
            _workingParticle = workingParticle;
            _openedParticle = openedParticle;
        }

        public void SetWorkingParticle(bool active)
        {
            if (active)
            {
                FxHelper.Play(_workingParticle);
            }
            else
            {
                FxHelper.Stop(_workingParticle);
            }
        }

        public void SetOpenedParticle(bool active)
        {
            if (active)
            {
                FxHelper.Play(_openedParticle);
            }
            else
            {
                FxHelper.Stop(_openedParticle);
            }
        }

        public void ClearAll()
        {
            FxHelper.Clear(_workingParticle);
            FxHelper.Clear(_openedParticle);
        }
    }
}
