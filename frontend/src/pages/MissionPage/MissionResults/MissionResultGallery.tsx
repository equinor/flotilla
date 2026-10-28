import { Button, Icon, Typography } from '@equinor/eds-core-react'
import { chevron_left, chevron_right, close } from '@equinor/eds-icons'
import { useLanguageContext } from 'contexts/LanguageContext'
import { KeyboardEvent } from 'react'
import { ResultMediaView } from './ResultMediaView'
import { GalleryResultContent } from './GalleryResultContent'
import {
    Gallery,
    GalleryBody,
    GalleryDescription,
    GalleryDetails,
    GalleryHeader,
    GalleryHeading,
    GalleryTaskNumber,
    Navigation,
    Strip,
    StripButton,
} from './MissionResultStyles'
import { MeasurementDisplay } from 'components/Displays/MeasurementDisplay'
import { ResultFocus } from './MissionResults'
import { groupInspectionsByTask, hasInspectionAnalysis, InspectionData } from 'models/InspectionRecord'

interface Props {
    data: InspectionData[]
    selectedId: string
    preferredFocus: ResultFocus
    installationName: string
    loadFailed: boolean
    onSelect: (id: string, focus?: ResultFocus) => void
    onClose: () => void
}

export const MissionResultGallery = ({
    data,
    selectedId,
    preferredFocus,
    installationName,
    loadFailed,
    onSelect,
    onClose,
}: Props) => {
    const { TranslateText } = useLanguageContext()
    const results = groupInspectionsByTask(data)
    const index = results.findIndex((result) => result.inspection.inspectionId === selectedId)
    const result = results[index]
    const inspection = result?.inspection
    const focus =
        inspection && preferredFocus === 'analysis' && hasInspectionAnalysis(inspection) ? 'analysis' : 'inspection'

    const navigate = (offset: number) => {
        const next = results[index + offset]
        if (index !== -1 && next) onSelect(next.inspection.inspectionId)
    }

    const onKeyDown = (event: KeyboardEvent<HTMLElement>) => {
        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
            event.preventDefault()
            navigate(event.key === 'ArrowLeft' ? -1 : 1)
        }
    }

    return (
        <Gallery open isDismissable onClose={onClose} onKeyDown={onKeyDown}>
            <GalleryHeader>
                <GalleryDetails>
                    <GalleryHeading>
                        <Typography variant="h3" tabIndex={-1}>
                            {inspection?.tag || TranslateText('Inspection results')}
                        </Typography>
                        {inspection && (
                            <GalleryTaskNumber variant="caption">
                                {TranslateText('Result task {0}', [String(index)])}
                            </GalleryTaskNumber>
                        )}
                    </GalleryHeading>
                    {inspection?.inspectionDescription && (
                        <GalleryDescription variant="body_long">{inspection.inspectionDescription}</GalleryDescription>
                    )}
                </GalleryDetails>
                <Navigation>
                    {index !== -1 && (
                        <Typography variant="caption">
                            {index + 1} / {results.length}
                        </Typography>
                    )}
                    <Button variant="ghost_icon" disabled={index <= 0} onClick={() => navigate(-1)}>
                        <Icon data={chevron_left} />
                    </Button>
                    <Button
                        variant="ghost_icon"
                        disabled={index === -1 || index === results.length - 1}
                        onClick={() => navigate(1)}
                    >
                        <Icon data={chevron_right} />
                    </Button>
                    <Button variant="ghost_icon" onClick={onClose}>
                        <Icon data={close} />
                    </Button>
                </Navigation>
            </GalleryHeader>
            {loadFailed && (
                <GalleryBody $paired={false}>
                    <Typography role="alert">{TranslateText('Failed to load inspection results')}</Typography>
                </GalleryBody>
            )}
            {inspection ? (
                <GalleryResultContent
                    inspection={inspection}
                    measurements={result.measurements}
                    focus={focus}
                    installationName={installationName}
                    onFocus={(newFocus) => {
                        onSelect(selectedId, newFocus)
                    }}
                />
            ) : !loadFailed ? (
                <GalleryBody $paired={false}>
                    <Typography role="alert">{TranslateText('Selected result is unavailable')}</Typography>
                </GalleryBody>
            ) : null}
            <Strip>
                <Typography variant="h6">{TranslateText('Inspections in this mission')}</Typography>
                <div>
                    {results.map(({ inspection, measurements }, inspectionIndex) => {
                        const id = inspection.inspectionId
                        const thumbnailSource = inspection.anonymizedSAS ?? inspection.visualizedSAS
                        return (
                            <StripButton
                                key={id}
                                $hasFinding={
                                    hasInspectionAnalysis(inspection) ? Boolean(inspection?.warning) : undefined
                                }
                                variant="ghost"
                                onClick={() => onSelect(id)}
                            >
                                {measurements.length === 0 && thumbnailSource && (
                                    <ResultMediaView
                                        source={thumbnailSource}
                                        label={inspection.tag}
                                        fileType={inspection.fileType}
                                        size="thumbnail"
                                    />
                                )}
                                {measurements.length === 1 && (
                                    <MeasurementDisplay recordId={measurements[0].recordId} isCompact={true} />
                                )}
                                {measurements.length > 1 && (
                                    <Typography variant="body_short">
                                        {TranslateText('{0} measurements', [String(measurements.length)])}
                                    </Typography>
                                )}
                                <Typography variant="body_short">{inspection.tag}</Typography>
                                <Typography variant="caption">
                                    {TranslateText('Result task {0}', [String(inspectionIndex)])}
                                </Typography>
                            </StripButton>
                        )
                    })}
                </div>
            </Strip>
        </Gallery>
    )
}
