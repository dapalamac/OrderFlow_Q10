import { useState } from "react";
import OrderForm from "./components/OrderForm";
import OrderList from "./components/OrderList";
import OrderSearch from "./components/OrderSearch";

function App() {
    const [refreshTrigger, setRefreshTrigger] = useState(0);

    function handleOrderCreated() {
        setRefreshTrigger((value) => value + 1);
    }

    return (
       <main>
          <div className="header">
              <h1>OrderFlow</h1>
              <p>Gestión de pedidos e inventario</p>
          </div>

            <OrderForm
                onOrderCreated={handleOrderCreated}
            />

            <OrderSearch />

            <OrderList
                refreshTrigger={refreshTrigger}
            />
        </main>
    );
}

export default App;