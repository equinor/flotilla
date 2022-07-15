import { Button } from '@equinor/eds-core-react'
import { useApi } from 'api/ApiCaller'
import { TaskTable } from 'components/TaskOverview/TaskTable'
import { VideoStreamWindow } from 'components/VideoStream/VideoStreamWindow'
import styled from 'styled-components'

const StyledMissionPage = styled.div`
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
    gap: 3rem;
`
const VideoStreamSection = styled.div`
    display: flex;
`

export function MissionPage() {
    const apiCaller = useApi()
    return (
        <StyledMissionPage>
            <VideoStreamSection>
                <VideoStreamWindow />
            </VideoStreamSection>
            <TaskTable />
            <Button href="..">FrontPage</Button>
        </StyledMissionPage>
    )
}
