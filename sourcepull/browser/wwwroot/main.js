import { dotnet } from './_framework/dotnet.js'

const { getAssemblyExports, getConfig } = await dotnet.create();
const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
await dotnet.runMain();

const sourcePull = exports.SourcePullBrowser;
const output = document.getElementById('output');
const status = document.getElementById('status');
const runButton = document.getElementById('run');

runButton.disabled = false;
status.textContent = 'Ready';
runButton.addEventListener('click', run);

async function run() {
    runButton.disabled = true;
    status.textContent = 'Running';
    output.textContent = '';

    try {
        const rows = [];
        const lineCounts = [1, 10, 100, 250, 500, 1000, 2500, 5000, 10000];
        const planNames = ['Rows', 'Count', 'Head10', 'FilteredHead10'];
        const variantNames = ['Complete', 'Oracle', 'Pull'];
        const iterations = 21;

        for (const lineCount of lineCounts) {
            const inputBytes = sourcePull.GetInputUtf8Bytes(lineCount);

            for (let planIndex = 0; planIndex < planNames.length; planIndex++) {
                const kernelRows = [];

                for (let variantIndex = 0; variantIndex < variantNames.length; variantIndex++) {
                    const values = sourcePull.MeasureKernel(
                        lineCount,
                        planIndex,
                        variantIndex,
                        iterations).split(',');

                    kernelRows.push({
                        corpus: `Lines${lineCount}`,
                        inputBytes,
                        plan: planNames[planIndex],
                        variant: variantNames[variantIndex],
                        microseconds: Number(values[0]),
                        linesScanned: Number(values[1]),
                        rowsMaterialized: Number(values[2]),
                        rowsEmitted: Number(values[3]),
                        pulls: Number(values[4]),
                        outputBytes: Number(values[5]),
                        checksum: values[6],
                        batchSize: Number(values[7])
                    });
                }

                const browser = await measureReadableStream(lineCount, planIndex, iterations);
                const browserRow = {
                    corpus: `Lines${lineCount}`,
                    inputBytes,
                    plan: planNames[planIndex],
                    variant: 'WebStream',
                    ...browser
                };
                assertEquivalent(kernelRows, browserRow);
                rows.push(...kernelRows, browserRow);
            }
        }

        function assertEquivalent(kernelRows, browserRow) {
            const pull = kernelRows.find(row => row.variant === 'Pull');

            for (const row of [...kernelRows, browserRow]) {
                if (row.outputBytes !== pull.outputBytes || row.checksum !== pull.checksum) {
                    throw new Error(
                        `${row.corpus}/${row.plan}/${row.variant} output differs from Pull.`);
                }
            }

            for (const property of ['linesScanned', 'rowsEmitted', 'pulls']) {
                if (browserRow[property] !== pull[property]) {
                    throw new Error(
                        `${browserRow.corpus}/${browserRow.plan}/WebStream ${property} ` +
                        `differs from Pull.`);
                }
            }
        }

        output.textContent = toCsv(rows);
        console.log(output.textContent);
        status.textContent = `PASS: ${rows.length} measurements`;
        document.documentElement.dataset.result = 'pass';
    } catch (error) {
        console.error(error);
        status.textContent = `FAIL: ${error}`;
        document.documentElement.dataset.result = 'fail';
    } finally {
        runButton.disabled = false;
    }
}

async function measureReadableStream(lineCount, planIndex, iterations) {
    const samples = [];
    let last;

    await consumeReadableStream(lineCount, planIndex);
    const calibrationSamples = [];

    for (let index = 0; index < 5; index++) {
        const calibrationStarted = performance.now();
        await consumeReadableStream(lineCount, planIndex);
        const elapsed = performance.now() - calibrationStarted;

        if (elapsed > 0) {
            calibrationSamples.push(elapsed);
        }
    }

    const calibrationMilliseconds = calibrationSamples.length === 0
        ? 0.001
        : Math.min(...calibrationSamples);
    const batchSize = Math.max(
        1,
        Math.min(1000, Math.ceil(20 / Math.max(calibrationMilliseconds, 0.001))));

    for (let index = 0; index < iterations; index++) {
        const started = performance.now();

        for (let repetition = 0; repetition < batchSize; repetition++) {
            last = await consumeReadableStream(lineCount, planIndex);
        }

        samples.push((performance.now() - started) * 1000 / batchSize);
    }

    samples.sort((left, right) => left - right);

    return {
        microseconds: samples[Math.floor(samples.length / 2)],
        linesScanned: last.linesScanned,
        rowsMaterialized: 0,
        rowsEmitted: last.rowsEmitted,
        pulls: last.pulls,
        outputBytes: last.outputBytes,
        checksum: last.checksum,
        batchSize
    };
}

async function consumeReadableStream(lineCount, planIndex) {
    const handle = sourcePull.Open(lineCount, planIndex, 4096);
    let managedBuffer;

    try {
        managedBuffer = sourcePull.GetBuffer(handle);

        if (sourcePull.IsStarted(handle)) {
            throw new Error('Opening a session started production.');
        }

        const stream = new ReadableStream({
            pull(controller) {
                const read = sourcePull.Pull(handle);

                if (read === 0) {
                    controller.close();
                    return;
                }

                controller.enqueue(managedBuffer.slice(0, read));
            },
            cancel() {
                sourcePull.Close(handle);
            }
        }, { highWaterMark: 0 });

        if (sourcePull.IsStarted(handle)) {
            throw new Error('Constructing a ReadableStream started production.');
        }

        const reader = stream.getReader();
        let outputBytes = 0;

        while (true) {
            const { value, done } = await reader.read();

            if (done) {
                break;
            }

            outputBytes += value.length;
        }

        return {
            linesScanned: sourcePull.GetLinesScanned(handle),
            rowsEmitted: sourcePull.GetRowsEmitted(handle),
            pulls: sourcePull.GetPullCount(handle),
            outputBytes,
            checksum: sourcePull.GetChecksum(handle)
        };
    } finally {
        sourcePull.Close(handle);
        managedBuffer?.dispose?.();
    }
}

function toCsv(rows) {
    const header =
        'Corpus,InputBytes,Plan,Variant,MedianUs,LinesScanned,RowsMaterialized,' +
        'RowsEmitted,Pulls,OutputBytes,Checksum,BatchSize';
    const lines = rows.map(row =>
        `${row.corpus},${row.inputBytes},${row.plan},${row.variant},` +
        `${row.microseconds.toFixed(3)},` +
        `${row.linesScanned},${row.rowsMaterialized},${row.rowsEmitted},` +
        `${row.pulls},${row.outputBytes},${row.checksum},${row.batchSize}`);
    return [header, ...lines].join('\n');
}
