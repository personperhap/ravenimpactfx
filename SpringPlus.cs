using System;
using UnityEngine;

// Token: 0x020001E7 RID: 487
public class SpringPlus
{
	// Token: 0x0600117F RID: 4479 RVA: 0x00060ED0 File Offset: 0x0005F0D0
	public SpringPlus(float spring, float drag, Vector3 min, Vector3 max, int iterations, float simSpeed, float distanceCoefficient)
	{
		this.spring = spring;
		this.drag = drag;
		this.position = Vector3.zero;
		this.velocity = Vector3.zero;
		this.min = min;
		this.max = max;
		this.iterations = iterations;
		this.simSpeed = simSpeed;
		this.distanceCoefficient = distanceCoefficient;
	}
	// Token: 0x06001180 RID: 4480 RVA: 0x00060F1E File Offset: 0x0005F11E
	public void AddVelocity(Vector3 delta)
	{
		this.velocity += delta;
	}

	// Token: 0x06001181 RID: 4481 RVA: 0x00060F34 File Offset: 0x0005F134
	public void Update()
	{
		float dist = Vector3.Distance(position, Vector3.zero) * distanceCoefficient;
		float d = (Time.deltaTime / (float)this.iterations) * simSpeed * (dist + 1);
		for (int i = 0; i < this.iterations; i++)
		{
			this.velocity -= (this.position * this.spring + this.velocity * this.drag) * d;
			this.position = Vector3.Min(Vector3.Max(this.position + this.velocity * d, this.min), this.max);
		}
		control -= Time.deltaTime;
		control = control < 0 ? 0 : control;
	}

	// Token: 0x04001190 RID: 4496
	public float spring;

	// Token: 0x04001191 RID: 4497
	public float drag;

	public float simSpeed = 1;

	// Token: 0x04001192 RID: 4498
	public Vector3 position;

	// Token: 0x04001193 RID: 4499
	public Vector3 velocity;

	// Token: 0x04001194 RID: 4500
	public Vector3 min;

	// Token: 0x04001195 RID: 4501
	public Vector3 max;

	// Token: 0x04001196 RID: 4502
	public int iterations;

	public float distanceCoefficient;

	public float control;
}
