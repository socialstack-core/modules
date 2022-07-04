using Api.Contexts;
using Api.Database;
using Api.Eventing;
using Api.Permissions;
using Api.Startup;
using Api.WebSockets;
using System;
using System.Threading.Tasks;

namespace Api.BlockDatabase;

/// <summary>
/// Stores network room meta for a given type.
/// </summary>
public class BlockNetworkRoomTypeStdMeta<T, ID, INST_T> : NetworkRoomTypeMeta
	where T : Content<ID>, new()
	where INST_T : T, new()
	where ID : struct, IConvertible, IEquatable<ID>, IComparable<ID>
{
	/// <summary>
	/// The service for the type.
	/// </summary>
	public AutoService<T, ID> Service;

	private ServiceCache<T, ID> _primaryCache;

	private EventHandler<T, int> _receivedEventHandler;

	private Capability _loadCapability;

	/// <summary>
	/// Gets the load capability from the host service.
	/// </summary>
	public override Capability LoadCapability
	{
		get
		{
			if (_loadCapability == null)
			{
				_loadCapability = Service.EventGroup.GetLoadCapability();
			}

			return _loadCapability;
		}
	}

	/// <summary>
	/// Creates new type meta.
	/// </summary>
	/// <param name="svc"></param>
	public BlockNetworkRoomTypeStdMeta(AutoService<T, ID> svc)
	{
		Service = svc;
		_primaryCache = Service.GetCacheForLocale(1);
		_receivedEventHandler = Service.EventGroup.Received;
	}

	/// <summary>
	/// Gets or creates the network room of the given ID.
	/// </summary>
	/// <param name="roomId"></param>
	public override NetworkRoom GetOrCreateRoom(ulong roomId)
	{
		return Service.StandardNetworkRooms.GetOrCreateRoom(Service.ConvertId(roomId));
	}

	/*
	private async ValueTask OnReceiveUpdate(int action, uint localeId, T entity)
	{
		try {
			// Update local cache next:
			var cache = Service.GetCacheForLocale(localeId);

			// The cache will be null if this is a non-cached type.
			// That can happen if it was identified that an object needed to be synced specifically for network rooms.
			if (cache != null)
			{
				// Create the context using role 1:
				var context = new Context(localeId, null, 1);

				T raw;

				if (context.LocaleId == 1)
				{
					// Primary locale. Entity == raw entity, and no transferring needs to happen.
					raw = entity;
				}
				else
				{
					// Get the raw entity from the cache. We'll copy the fields from the raw object to it.
					raw = cache.GetRaw(entity.Id);

					if (raw == null)
					{
						raw = new INST_T();
					}

					// Transfer fields from entity to raw, using the primary object as a source of blank fields.
					// If fields on the raw object were changed, this makes sure they're up to date.
					Service.PopulateRawEntityFromTarget(entity, raw, _primaryCache.Get(raw.Id));
				}

				// Received the content object:
				await _receivedEventHandler.Dispatch(context, entity, action);
					
				lock (cache)
				{
					if (action == 1 || action == 2)
					{
						// Created or updated
						cache.Add(context, entity, raw);

						if (context.LocaleId == 1)
						{
							// Primary locale update - must update all other caches in case they contain content from the primary locale.
							Service.OnPrimaryEntityChanged(entity);
						}
					}
					else if (action == 3)
					{
						// Deleted
						cache.Remove(context, entity.Id);
					}
				}
			}

			// Network room update next.
			// The 20 is because action 1 (create) maps to opcode 21. Action 2 => 22 and action 3 => 23.
			var room = Service.StandardNetworkRooms.GetRoom(entity.Id);

			NetworkRoom<T, ID, ID> globalMsgRoom = Service.StandardNetworkRooms.AnyUpdateRoom;

			if (room != null)
			{
				await room.SendLocallyIfPermitted(entity, (byte)(action + 20));
			}

			if (globalMsgRoom != null)
			{
				await globalMsgRoom.SendLocallyIfPermitted(entity, (byte)(action + 20));
			}

		}
		catch(Exception e)
		{
			Console.WriteLine("Sync non-fatal error:" + e.ToString());
		}
	}
	*/

}


/// <summary>
/// Stores network room meta for a given mapping type.
/// </summary>
public class BlockNetworkRoomTypeMappingMeta<SRC_ID, TARG_ID, INST_T> : NetworkRoomTypeMeta
	where SRC_ID : struct, IEquatable<SRC_ID>, IConvertible, IComparable<SRC_ID>
	where TARG_ID : struct, IEquatable<TARG_ID>, IConvertible, IComparable<TARG_ID>
	where INST_T : Mapping<SRC_ID, TARG_ID>, new()
{
	/// <summary>
	/// The service for the type.
	/// </summary>
	public MappingService<SRC_ID, TARG_ID> Service;

	private ServiceCache<Mapping<SRC_ID, TARG_ID>, uint> cache;

	private EventHandler<Mapping<SRC_ID, TARG_ID>, int> _receivedEventHandler;

	private Context context;

	private Capability _loadCapability;

	/// <summary>
	/// True if this is a mapping type.
	/// </summary>
	public override bool IsMapping
	{
		get
		{
			return true;
		}
	}

	/// <summary>
	/// Gets the load capability from the host service.
	/// </summary>
	public override Capability LoadCapability
	{
		get
		{
			if (_loadCapability == null)
			{
				_loadCapability = Service.EventGroup.GetLoadCapability();
			}

			return _loadCapability;
		}
	}
	
	/// <summary>
	/// Creates new type meta.
	/// </summary>
	/// <param name="svc"></param>
	public BlockNetworkRoomTypeMappingMeta(MappingService<SRC_ID, TARG_ID> svc)
	{
		Service = svc;
		
		cache = Service.GetCacheForLocale(1);
		_receivedEventHandler = Service.EventGroup.Received;

		// Mappings are always locale free.
		context = new Context(1, null, 1);


		if (typeof(SRC_ID) == typeof(uint))
		{
			_srcIdConverter = new UInt32IDConverter() as IDConverter<SRC_ID>;
		}
		else if (typeof(SRC_ID) == typeof(ulong))
		{
			_srcIdConverter = new UInt64IDConverter() as IDConverter<SRC_ID>;
		}
		else
		{
			throw new ArgumentException("Currently unrecognised ID type: ", nameof(SRC_ID));
		}

	}

	private IDConverter<SRC_ID> _srcIdConverter;

	/*
	private async ValueTask OnReceiveUpdate(int action, uint localeId, INST_T entity)
	{
		try
		{
			// Update local cache next.

			// The cache will be null if this is a non-cached type.
			// That can happen if it was identified that an object needed to be synced specifically for network rooms.
			if (cache != null)
			{
				Mapping<SRC_ID, TARG_ID> raw = entity;

				// Received the content object:
				await _receivedEventHandler.Dispatch(context, entity, action);
				
				lock (cache)
				{
					if (action == 1 || action == 2)
					{
						// Created or updated
						cache.Add(context, entity, raw);
								
						// Primary locale update - must update all other caches in case they contain content from the primary locale.
						Service.OnPrimaryEntityChanged(entity);
					}
					else if (action == 3)
					{
						// Deleted
						cache.Remove(context, entity.Id);
					}
				}
			}

			// Network room update next.
			// The 20 is because action 1 (create) maps to opcode 21. Action 2 => 22 and action 3 => 23.
			var room = Service.MappingNetworkRooms.GetRoom(entity.SourceId);

			if (room != null)
			{
				await room.SendLocallyIfPermitted(entity, (byte)(action + 20));
			}

		}
		catch (Exception e)
		{
			Console.WriteLine("Sync non-fatal error:" + e.ToString());
		}
	}
	*/

	/// <summary>
	/// Gets or creates the network room of the given ID.
	/// </summary>
	/// <param name="roomId"></param>
	public override NetworkRoom GetOrCreateRoom(ulong roomId)
	{
		return Service.MappingNetworkRooms.GetOrCreateRoom(_srcIdConverter.Convert(roomId));
	}
	
}
