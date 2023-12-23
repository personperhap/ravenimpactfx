using System;
using UnityEngine;

// Token: 0x020001EC RID: 492
public struct TimedActionPlus
{
	// Token: 0x06001192 RID: 4498 RVA: 0x0006136C File Offset: 0x0005F56C
	public TimedActionPlus(float lifetime, bool unscaledTime = false)
	{
		this.lifetime = lifetime;
		this.end = 0f;
		this.lied = true;
		this.unscaledTime = unscaledTime;
	}

	// Token: 0x06001193 RID: 4499 RVA: 0x0006138E File Offset: 0x0005F58E
	public void Start()
	{
		this.end = this.GetTime() + this.lifetime;
		this.lied = false;
	}

	// Token: 0x06001194 RID: 4500 RVA: 0x000613AA File Offset: 0x0005F5AA
	public void StartLifetime(float lifetime)
	{
		this.lifetime = lifetime;
		this.Start();
	}

	// Token: 0x06001195 RID: 4501 RVA: 0x000613B9 File Offset: 0x0005F5B9
	public void Stop()
	{
		this.end = 0f;
		this.lied = true;
	}

	// Token: 0x06001196 RID: 4502 RVA: 0x000613CD File Offset: 0x0005F5CD
	public float Remaining()
	{
		return this.end - this.GetTime();
	}

	// Token: 0x06001197 RID: 4503 RVA: 0x000613DC File Offset: 0x0005F5DC
	public float Elapsed()
	{
		return this.lifetime - this.Remaining();
	}

	// Token: 0x06001198 RID: 4504 RVA: 0x000613EB File Offset: 0x0005F5EB
	public float Ratio()
	{
		return Mathf.Clamp01(1f - (this.end - this.GetTime()) / this.lifetime);
	}

	// Token: 0x06001199 RID: 4505 RVA: 0x0006140C File Offset: 0x0005F60C
	public bool TrueDone()
	{
		return this.end <= this.GetTime();
	}

	// Token: 0x0600119A RID: 4506 RVA: 0x0006141F File Offset: 0x0005F61F
	private float GetTime()
	{
		if (!this.unscaledTime)
		{
			return Time.time;
		}
		return Time.unscaledTime;
	}

	// Token: 0x0600119B RID: 4507 RVA: 0x00061434 File Offset: 0x0005F634
	public bool Done()
	{
		if (!this.TrueDone())
		{
			return false;
		}
		if (!this.lied)
		{
			this.lied = true;
			return false;
		}
		return true;
	}

	// Token: 0x040011A6 RID: 4518
	public float lifetime;

	// Token: 0x040011A7 RID: 4519
	public float end;

	// Token: 0x040011A8 RID: 4520
	private bool unscaledTime;

	// Token: 0x040011A9 RID: 4521
	private bool lied;
}
