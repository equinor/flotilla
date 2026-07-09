import { useInspectionsContext } from 'components/Contexts/InspectionsContext'
import { StyledInspection, StyledInspectionImage } from './InspectionStyles'
import { tokens } from '@equinor/eds-tokens'
import { CircularProgress, Typography } from '@equinor/eds-core-react'
import styled from 'styled-components'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { VideoPlaceholder, VideoPlayer } from './InspectionVideoPlayer'
import { FileType, InspectionData } from 'models/InspectionRecord'

const StyledSmallImagePlaceholder = styled.div`
    display: flex;
    justify-content: center;
    align-items: flex-start;
    padding: 16px 8px;
    height: 100%;
`
const StyledLargeImagePlaceholder = styled.div`
    display: flex;
    flex-direction: column;
    align-items: center;
    padding: 16px;
    gap: 16px;
`
const StyledCircularProgress = styled.div`
    display: flex;
    align-items: center;
    align-self: center;
    padding: 16px;
    height: 100%;
`

export const TextAsImage = ({
    isLargeImage,
    text,
    textArgs,
}: {
    isLargeImage: boolean
    text: string
    textArgs?: string[]
}) => {
    const { TranslateText } = useLanguageContext()

    if (isLargeImage) {
        return (
            <StyledLargeImagePlaceholder>
                <Typography variant="h3" color={tokens.colors.text.static_icons__tertiary.hex}>
                    {TranslateText(text, textArgs)}
                </Typography>
            </StyledLargeImagePlaceholder>
        )
    }

    return (
        <StyledSmallImagePlaceholder>
            <Typography color={tokens.colors.text.static_icons__tertiary.hex}>
                {TranslateText(text, textArgs)}
            </Typography>
        </StyledSmallImagePlaceholder>
    )
}

export const PendingResultPlaceholder = ({ isLargeImage }: { isLargeImage: boolean }) => {
    const { TranslateText } = useLanguageContext()

    if (isLargeImage) {
        return (
            <StyledLargeImagePlaceholder>
                <Typography variant="h3" color={tokens.colors.text.static_icons__tertiary.hex}>
                    {TranslateText('Waiting for inspection result')}
                </Typography>
                <CircularProgress size={48} />
            </StyledLargeImagePlaceholder>
        )
    }

    return (
        <StyledCircularProgress>
            <CircularProgress />
        </StyledCircularProgress>
    )
}

export const InspectionImageWithPlaceholder = ({
    inspection,
    isLargeImage,
}: {
    inspection: InspectionData
    isLargeImage: boolean
}) =>
    isLargeImage ? (
        <StyledInspection $otherContentHeight={'174px'} src={inspection.anonymizedSAS} />
    ) : (
        <StyledInspectionImage src={inspection.anonymizedSAS} />
    )

const InspectionValueWithPlaceholder = ({
    inspection,
    isLargeImage,
}: {
    inspection: InspectionData
    isLargeImage: boolean
}) => {
    if (!inspection.value) {
        const errorMsg = 'No inspection could be found'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else {
        return (
            <TextAsImage
                isLargeImage={isLargeImage}
                text={`${inspection.inspectionDescription}: {0} ${inspection.unit}`}
                textArgs={[parseFloat(inspection.value).toFixed(3).toString()]}
            />
        )
    }
}

const InspectionVideoWithPlaceholder = ({
    inspection,
    isLargeImage,
}: {
    inspection: InspectionData
    isLargeImage: boolean
}) => (isLargeImage ? <LargeVideoWithPlaceholder inspection={inspection} /> : <VideoPlaceholder />)

const LargeVideoWithPlaceholder = ({ inspection }: { inspection: InspectionData }) => (
    <VideoPlayer src={inspection.anonymizedSAS} />
)

const InspectionResultWithPlaceholder = ({
    inspection,
    isLargeImage,
}: {
    inspection: InspectionData
    isLargeImage: boolean
}) => {
    if (inspection.fileType === FileType.SOUND) {
        const errorMsg = 'Viewing of the inspection type is not supported'
        return <TextAsImage isLargeImage={isLargeImage} text={errorMsg} />
    } else if (inspection.fileType === FileType.VALUE) {
        return <InspectionValueWithPlaceholder inspection={inspection} isLargeImage={isLargeImage} />
    } else if (inspection.fileType === FileType.IMAGE) {
        return <InspectionImageWithPlaceholder inspection={inspection} isLargeImage={isLargeImage} />
    } else if (inspection.fileType === FileType.VIDEO) {
        return <InspectionVideoWithPlaceholder inspection={inspection} isLargeImage={isLargeImage} />
    }
}

export const LargeDialogInspectionResult = ({ inspection }: { inspection: InspectionData }) => (
    <InspectionResultWithPlaceholder inspection={inspection} isLargeImage={true} />
)

export const SmallInspectionResult = ({ inspection }: { inspection: InspectionData }) => (
    <InspectionResultWithPlaceholder inspection={inspection} isLargeImage={false} />
)

const AnalysisImageWithPlaceholder = ({
    inspectionId,
    isLargeImage,
}: {
    inspectionId: string
    isLargeImage: boolean
}) => {
    const { useSaraData } = useInspectionsContext()
    const { data, isPending, isError } = useSaraData(inspectionId)

    if (!data?.visualisedSAS) {
        return <TextAsImage isLargeImage={isLargeImage} text={'No analysis available'} />
    } else if (isPending) {
        return <PendingResultPlaceholder isLargeImage={isLargeImage} />
    } else if (isError || !data) {
        return <TextAsImage isLargeImage={isLargeImage} text={'No analysis could be found'} />
    } else
        return isLargeImage ? (
            <StyledInspection $otherContentHeight={'174px'} src={data.visualisedSAS} />
        ) : (
            <StyledInspectionImage src={data.visualisedSAS} />
        )
}

export const SmallAnalysisResult = ({ inspectionId }: { inspectionId: string }) => (
    <AnalysisImageWithPlaceholder inspectionId={inspectionId} isLargeImage={false} />
)
