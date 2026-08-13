import { useState } from "react";
import { getOrderById } from "../services/orderService";

function OrderSearch() {
    const [id, setId] = useState("");
    const [order, setOrder] = useState(null);
    const [error, setError] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSearch(event) {
        event.preventDefault();

        setError("");
        setOrder(null);

        if (!id.trim()) {
            setError("Debes ingresar un ID de pedido.");
            return;
        }

        try {
            setLoading(true);

            const data = await getOrderById(id.trim());

            setOrder(data);
        } catch (error) {
            setError("No se encontró el pedido.");
        } finally {
            setLoading(false);
        }
    }

    return (
        <section>
            <h2>Buscar pedido</h2>

            <form onSubmit={handleSearch}>
                <div>
                    <label>ID del pedido</label>

                    <input
                        type="text"
                        value={id}
                        onChange={(event) =>
                            setId(event.target.value)
                        }
                        placeholder="Ej. 392c5eef-f2f0-4c83-a00f-c4d8a4c5b45f"
                    />
                </div>

                {error && (
                    <p className="error">
                        {error}
                    </p>
                )}

                <button
                    type="submit"
                    disabled={loading}
                >
                    {loading ? "Buscando..." : "Buscar pedido"}
                </button>
            </form>

            {order && (
                <div>
                    <h3>Detalle del pedido</h3>

                    <p>
                        <strong>ID:</strong> {order.id}
                    </p>

                    <p>
                        <strong>Cliente:</strong>{" "}
                        {order.customerName}
                    </p>

                    <p>
                        <strong>SKU:</strong> {order.sku}
                    </p>

                    <p>
                        <strong>Cantidad:</strong>{" "}
                        {order.quantity}
                    </p>

                    <p>
                        <strong>Estado:</strong>{" "}
                        {getStatusText(order.status)}
                    </p>

                    <p>
                        <strong>Fecha:</strong>{" "}
                        {new Date(`${order.createdAt}Z`).toLocaleString(
                            "es-CO",
                            {
                                timeZone: "America/Bogota",
                                dateStyle: "short",
                                timeStyle: "medium"
                            }
                        )}
                    </p>
                </div>
            )}
        </section>
    );
}

function getStatusText(status) {
    switch (status) {
        case 0:
            return "Pending";

        case 1:
            return "Confirmed";

        case 2:
            return "Rejected";

        default:
            return "Unknown";
    }
}

export default OrderSearch;