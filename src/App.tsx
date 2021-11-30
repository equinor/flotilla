import { Icon, Typography } from '@equinor/eds-core-react';
import { platform } from '@equinor/eds-icons';

Icon.add({ platform });

function App() {
    return (<>
        <Typography color="primary" variant="h1" bold>Flotilla</Typography>
        <Icon name="platform" size={48} />
        <Typography variant="body_short" >Flotilla is the main point of access for operators to interact with multiple robots in a facility.</Typography>
    </>)
}

export default App
