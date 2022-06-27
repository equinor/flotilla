import { Button } from "@equinor/eds-core-react"
import { useApi } from "api/ApiCaller"
import { VideoStreamWindow } from "components/VideoStream/VideoStreamWindow"
import styled from "styled-components"

const VideoStreamSection = styled.div`
    display: flex;
`


export function MissionPage() {
    const apiCaller = useApi()
    return (
        <div>
        <VideoStreamSection>
            <VideoStreamWindow />
        </VideoStreamSection>
        <Button href="..">FrontPage</Button>
        </div>
    )
}