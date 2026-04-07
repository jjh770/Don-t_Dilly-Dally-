using System;
using DG.Tweening;

public class RewardSequenceBuilder
{
    private readonly RewardView _view;
    private readonly Sequence _seq;

    public RewardSequenceBuilder(RewardView view, Sequence seq)
    {
        _view = view;
        _seq = seq;
    }

    public RewardSequenceBuilder SliderFill(float ratio)
    {
        _view.Step_SliderFill(_seq, ratio);
        return this;
    }

    public RewardSequenceBuilder StarReveal(int count)
    {
        _view.Step_StarReveal(_seq, count);
        return this;
    }

    public RewardSequenceBuilder CoinCount(int coin)
    {
        _view.Step_CoinCount(_seq, coin);
        return this;
    }

    public RewardSequenceBuilder StarCount(int star)
    {
        _view.Step_StarCount(_seq, star);
        return this;
    }

    public RewardSequenceBuilder SummaryTyping()
    {
        _view.Step_SummaryTyping(_seq);
        return this;
    }

    public RewardSequenceBuilder MoneyFadeIn()
    {
        _view.Step_MoneyFadeIn(_seq);
        return this;
    }

    public RewardSequenceBuilder Interval(float seconds)
    {
        _seq.AppendInterval(seconds);
        return this;
    }

    public RewardSequenceBuilder OnComplete(Action callback)
    {
        _seq.OnComplete(() => callback?.Invoke());
        return this;
    }

    public Sequence Play()
    {
        return _seq.Play();
    }
}
