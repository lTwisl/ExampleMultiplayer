using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public class MovementController : NetworkBehaviour
{
    [SerializeField] private float _maxSpeed = 6.0f;
    [SerializeField] private float _acceleration = 4.0f;
    [SerializeField] private float _speedRotate = 10.0f;

    [SerializeField] private GameObject _spawnedObjectPrefab;

    public NetworkVariable<int> _networkVariable = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float _speed;

    private CharacterController _characterController;
    private Animator _animator;
    private Vector3 _input;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        _networkVariable.OnValueChanged += (int previusValue, int newValue) => { Debug.Log(OwnerClientId + "; Network variable = " + newValue); };
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            //GameObject spawnedObject = Instantiate(_spawnedObjectPrefab);
            //spawnedObject.GetComponent<NetworkObject>().Spawn(true);

            //var clientRpcParans = new ClientRpcParams()
            //{
            //    Send = new ClientRpcSendParams()
            //    {
            //        TargetClientIds = new List<ulong> { 1 }
            //    }
            //};
            //TestClientRpc(clientRpcParans);

            TestServerRpc(new ServerRpcParams());

            //_networkVariable.Value = Random.Range(0, 100);
        }

        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0.0f, Input.GetAxisRaw("Vertical"));

        if (_input != Vector3.zero)
        {
            if (_maxSpeed - _speed > 0.001f)
                _speed = Mathf.Lerp(_speed, _maxSpeed, Time.deltaTime * _acceleration);
        }
        else
        {
            if (_speed > 0.001f)
                _speed = Mathf.Lerp(_speed, 0, Time.deltaTime * _acceleration * 3);
        }

        Vector3 inputNorm = _input.normalized;

        _characterController.Move(_input.normalized * _speed * Time.deltaTime);

        if (Vector3.Angle(transform.forward, inputNorm) > 0.001f)
            transform.forward = Vector3.Slerp(transform.forward, inputNorm, Time.deltaTime * _speedRotate);

        _animator.SetFloat("Speed", _speed);
    }

    [ServerRpc]
    private void TestServerRpc(ServerRpcParams serverRpcParams)
    {
        Debug.Log("TestServerRpc " + OwnerClientId + "; " + serverRpcParams.Receive.SenderClientId);

        GameObject spawnedObject = Instantiate(_spawnedObjectPrefab);
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
    }

    [ClientRpc]
    private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log("TestClientRpc");
    }
}
