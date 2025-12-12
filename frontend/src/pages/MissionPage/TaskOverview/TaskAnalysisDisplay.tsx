import { Button, Dialog, Icon, Typography } from '@equinor/eds-core-react'
import { useState } from 'react'
import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { Inspection } from 'models/Inspection'
import styled from 'styled-components'
import {
    LargeImageErrorPlaceholder,
    LargeImagePendingPlaceholder,
} from 'pages/InspectionReportPage/InspectionReportImage'

const StyledDialog = styled(Dialog)`
    display: flex;
    max-width: 90vh;
    width: 100%;
    max-height: 80vh;
    gap: 0px;
    padding: 0px;
`
StyledDialog.Header = styled(Dialog.Header)`
    display: flex;
    height: 100%;
    border-bottom: none;
`
StyledDialog.Content = styled(Dialog.Content)`
    padding-right: 0px;
`
const StyledImage = styled.img<{ $otherContentHeight?: string }>`
    max-height: calc(80vh - ${(props) => props.$otherContentHeight});
    max-width: 100%;
    border: none;
`

const AnalysisImage = ({ inspectionId }: { inspectionId: string }) => {
    const { fetchAnalysisData } = useInspectionsContext()
    const { data, isPending } = fetchAnalysisData(inspectionId)

    if (isPending) return <LargeImagePendingPlaceholder />
    if (!data) return <LargeImageErrorPlaceholder errorMessage="No inspection could be found" />

    return <StyledImage $otherContentHeight="0px" src={data} />
}

const formatAnalysisType = (type: string) => type.charAt(0).toUpperCase() + type.slice(1).toLowerCase()

export const TaskAnalysisDisplay = ({ inspection }: { inspection: Inspection }) => {
    const [dialogOpen, setDialogOpen] = useState(false)
    const analysis = inspection.analysisResult

    const handleOpenDialog = () => setDialogOpen(true)
    const handleCloseDialog = () => setDialogOpen(false)

    return (
        <>
            {analysis?.analysisType && (
                <Button variant="ghost" color="danger" onClick={handleOpenDialog}>
                    <Typography link color="danger">
                        {formatAnalysisType(analysis.analysisType)}
                    </Typography>
                </Button>
            )}
            <StyledDialog open={dialogOpen} isDismissable onClose={handleCloseDialog}>
                <StyledDialog.Content>
                    <AnalysisImage inspectionId={inspection.isarInspectionId} />
                </StyledDialog.Content>
                <StyledDialog.Header>
                    <Button variant="ghost" onClick={handleCloseDialog}>
                        <Icon name="close" />
                    </Button>
                </StyledDialog.Header>
            </StyledDialog>
        </>
    )
}
