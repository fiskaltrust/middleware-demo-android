package eu.fiskaltrust.middleware.demo;

import fiskaltrust.ifPOS.v1.IPOS;
import fiskaltrust.ifPOS.v1.POSGrpc;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.concurrent.TimeUnit;

public class PosClient {

    private final POSGrpc.POSBlockingStub blockingStub;
    private final ManagedChannel channel;

    public PosClient(String url) {
        channel = ManagedChannelBuilder.forTarget(url)
                .usePlaintext()
                .build();
        blockingStub = POSGrpc.newBlockingStub(channel);
    }

    public void shutdown() throws InterruptedException {
        channel.shutdownNow().awaitTermination(5, TimeUnit.SECONDS);
    }

    public String echo(String message) {
        IPOS.EchoRequest request = IPOS.EchoRequest.newBuilder().setMessage(message).build();
        IPOS.EchoResponse response = blockingStub.echo(request);
        return response.getMessage();
    }

    public String journal(long journalType) {
        IPOS.JournalRequest request = IPOS.JournalRequest.newBuilder().setFtJournalType(journalType).build();
        Iterator<IPOS.JournalResponse> responses = blockingStub.journal(request);

        ArrayList<Integer> chunks = new ArrayList<>();
        responses.forEachRemaining(response -> chunks.addAll(response.getChunkList()));

        byte[] bytes = new byte[chunks.size()];
        for (int i = 0; i < chunks.size(); i++) {
            bytes[i] = chunks.get(i).byteValue();
        }

        return new String(bytes);
    }

    public IPOS.ReceiptResponse sign(IPOS.ReceiptRequest receiptRequest) {
        return blockingStub.sign(receiptRequest);
    }
}