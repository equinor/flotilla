import { Button } from '@equinor/eds-core-react'
import { useApi } from './ApiCaller'

export function ProfileContent() {
    const apiCaller = useApi()
    return (
        <>
            <h5 className="card-title">Welcome</h5>
            <Button
                variant="contained"
                onClick={() => {
                    apiCaller.getRobots()
                }}
            >
                Test Backend
            </Button>
        </>
    )
}
