using System;
using UnityEngine;
/*
public class AIBase : MonoBehaviour
{
	public float senseRange = 6f;

	private CharacterData data;

	private GeneralInput input;

	private Vector3 moveDir = Vector3.zero;

	private Vector3 aimDir = Vector3.zero;

	private Vector3 targetPos;

	private Player target;

	private bool canSeeTarget;

	private bool isShooting;

	private bool isJumping;

	private float distanceToTarget;

	public float senseDelay = 0.1f;

	private float startTime = -10f;

	protected private void Start()
	{
		this.data = base.GetComponentInParent<CharacterData>();
		this.input = this.data.input;
		this.ResetTimer();

		this.OnStart();
	}
	public virtual void OnStart()
    {

    }

	private void Update()
	{
		this.target = PlayerManager.instance.GetOtherPlayer(this.data.player);
		this.untilNextDataUpdate -= TimeHandler.deltaTime;
		if (this.target)
		{
			this.canSeeTarget = true;
			this.input.ResetInput();
			if (this.untilNextDataUpdate <= 0f)
			{
				if (UnityEngine.Random.value < 0.25f / Mathf.Clamp(this.distanceToTarget * 0.1f, 0.1f, 10f) && this.canSeeTarget)
				{
					this.input.shieldWasPressed = true;
				}
				if (UnityEngine.Random.value < 0.4f && this.canSeeTarget)
				{
					this.isShooting = true;
				}
				else
				{
					this.isShooting = false;
				}
				if (UnityEngine.Random.value < 0.2f || this.data.isWallGrab)
				{
					this.input.jumpWasPressed = true;
				}
				this.untilNextDataUpdate = UnityEngine.Random.Range(0f, 0.25f);
				this.UpdateData();
			}
			this.input.shootIsPressed = this.isShooting;
			this.input.shootWasPressed = this.isShooting;
			this.input.aimDirection = this.aimDir;
			this.input.direction = this.moveDir;
		}
	}

	private void GetRandomPos()
	{
		Vector3 lhs = Vector3.zero;
		int num = 200;
		while (lhs == Vector3.zero && num > 0)
		{
			num--;
			Vector3 vector = base.transform.position + Vector3.up * 5f + (Vector3)UnityEngine.Random.insideUnitCircle * 15f;
			if (this.data.ThereIsGroundBelow(vector, 8f))
			{
				lhs = vector;
			}
		}
		this.targetPos = lhs;
	}

	private void UpdateData()
	{
		if (this.canSeeTarget)
		{
			this.targetPos = this.target.transform.position;
		}
		this.distanceToTarget = Vector3.Distance(base.transform.position, this.target.transform.position);
		this.aimDir = (this.targetPos - base.transform.position).normalized;
		this.moveDir = this.aimDir;
		if (this.moveDir.x > 0f)
		{
			this.moveDir.x = 1f;
		}
		if (this.moveDir.x < 0f)
		{
			this.moveDir.x = -1f;
		}
		if (this.canSeeTarget && this.distanceToTarget < this.senseRange && this.data.ThereIsGroundBelow(base.transform.position, 10f))
		{
			this.moveDir = Vector3.zero;
		}
	}

	private void ResetTimer()
    {
		this.startTime = Time.time;
    }

	
}*/
/*
public class CustomAI : MonoBehaviour
{
	private void Start()
	{
		this.data = base.GetComponentInParent<CharacterData>();
		this.input = this.data.input;
	}

	private void Update()
	{
		this.target = PlayerManager.instance.GetOtherPlayer(this.data.player);
		this.untilNextDataUpdate -= TimeHandler.deltaTime;
		if (this.target)
		{
			this.canSeeTarget = true;
			this.input.ResetInput();
			if (this.untilNextDataUpdate <= 0f)
			{
				if (UnityEngine.Random.value < 0.25f / Mathf.Clamp(this.distanceToTarget * 0.1f, 0.1f, 10f) && this.canSeeTarget)
				{
					this.input.shieldWasPressed = true;
				}
				if (UnityEngine.Random.value < 0.4f && this.canSeeTarget)
				{
					this.isShooting = true;
				}
				else
				{
					this.isShooting = false;
				}
				if (UnityEngine.Random.value < 0.2f || this.data.isWallGrab)
				{
					this.input.jumpWasPressed = true;
				}
				this.untilNextDataUpdate = UnityEngine.Random.Range(0f, 0.25f);
				this.UpdateData();
			}
			this.input.shootIsPressed = this.isShooting;
			this.input.shootWasPressed = this.isShooting;
			this.input.aimDirection = this.aimDir;
			this.input.direction = this.moveDir;
		}
	}

	private void GetRandomPos()
	{
		Vector3 lhs = Vector3.zero;
		int num = 200;
		while (lhs == Vector3.zero && num > 0)
		{
			num--;
			Vector3 vector = base.transform.position + Vector3.up * 5f + (Vector3)UnityEngine.Random.insideUnitCircle * 15f;
			if (this.data.ThereIsGroundBelow(vector, 8f))
			{
				lhs = vector;
			}
		}
		this.targetPos = lhs;
	}

	private void UpdateData()
	{
		if (this.canSeeTarget)
		{
			this.targetPos = this.target.transform.position;
		}
		this.distanceToTarget = Vector3.Distance(base.transform.position, this.target.transform.position);
		this.aimDir = (this.targetPos - base.transform.position).normalized;
		this.moveDir = this.aimDir;
		if (this.moveDir.x > 0f)
		{
			this.moveDir.x = 1f;
		}
		if (this.moveDir.x < 0f)
		{
			this.moveDir.x = -1f;
		}
		if (this.canSeeTarget && this.distanceToTarget < this.range && this.data.ThereIsGroundBelow(base.transform.position, 10f))
		{
			this.moveDir = Vector3.zero;
		}
	}

	private float range = 6f;

	private CharacterData data;

	private GeneralInput input;

	private Vector3 moveDir = Vector3.zero;

	private Vector3 aimDir = Vector3.zero;

	private Vector3 targetPos;

	private Player target;

	private bool canSeeTarget;

	private float untilNextDataUpdate;

	private float getRandomTargetPosCounter;

	private bool isShooting;

	private float distanceToTarget = 5f;
}*/
