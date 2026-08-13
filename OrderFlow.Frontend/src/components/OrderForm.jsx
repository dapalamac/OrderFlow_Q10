import { useState } from "react";
import { createOrder } from "../services/orderService";

function OrderForm({ onOrderCreated }) {
    const [customerName, setCustomerName] = useState("");
    const [sku, setSku] = useState("");
    const [quantity, setQuantity] = useState("");

    const [error, setError] = useState("");
    const [success, setSuccess] = useState("");
    const [loading, setLoading] = useState(false);

    async function handleSubmit(event) {
        event.preventDefault();

        setError("");
        setSuccess("");

        if (!customerName.trim()) {
            setError("El nombre del cliente es obligatorio.");
            return;
        }

        if (!sku.trim()) {
            setError("El SKU es obligatorio.");
            return;
        }

        if (!quantity || Number(quantity) <= 0) {
            setError("La cantidad debe ser mayor que 0.");
            return;
        }

        try {
            setLoading(true);

            await createOrder({
                customerName: customerName.trim(),
                sku: sku.trim(),
                quantity: Number(quantity)
            });

            setSuccess("Pedido creado correctamente.");

            setCustomerName("");
            setSku("");
            setQuantity("");

            if (onOrderCreated) {
                onOrderCreated();
            }
        } catch (error) {
            setError(error.message);
        } finally {
            setLoading(false);
        }
    }

    return (
        <section>
            <h2>Crear pedido</h2>

            <form onSubmit={handleSubmit}>
                <div>
                    <label>Cliente</label>

                    <input
                        type="text"
                        value={customerName}
                        onChange={(event) =>
                            setCustomerName(event.target.value)
                        }
                        placeholder="Nombre del cliente"
                    />
                </div>

                <div>
                    <label>SKU</label>

                    <input
                        type="text"
                        value={sku}
                        onChange={(event) =>
                            setSku(event.target.value)
                        }
                        placeholder="Ej. ABC-01"
                    />
                </div>

                <div>
                    <label>Cantidad</label>

                    <input
                        type="number"
                        min="1"
                        value={quantity}
                        onChange={(event) =>
                            setQuantity(event.target.value)
                        }
                        placeholder="Cantidad"
                    />
                </div>

                {error && (
                    <p>
                        {error}
                    </p>
                )}

                {success && (
                    <p>
                        {success}
                    </p>
                )}

                <button
                    type="submit"
                    disabled={loading}
                >
                    {loading ? "Creando..." : "Crear pedido"}
                </button>
            </form>
        </section>
    );
}

export default OrderForm;