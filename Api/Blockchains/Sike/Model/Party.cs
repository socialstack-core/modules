
namespace Lumity.SikeIsogeny;

/**
 * A party involved in SIDH or SIKE communication.
 *
 * In case of SIKE, Bob is the client and Alice is the server.
 *
 * @author Roman Strobl, roman.strobl@wultra.com
 */
public enum Party {
    /// <summary>
    /// Server
    /// </summary>
    ALICE,
    /// <summary>
    /// Client
    /// </summary>
    BOB
}
